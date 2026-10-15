using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DatabaseTools.DbTools;
using DatabaseTools.DbTools.Models;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibDatabaseParameters;
using ParametersManagement.LibFileParameters.Models;
using Polly;
using ReplicatorShared.Data.Models;
using ReplicatorShared.Data.StepCommands;
using ReplicatorShared.Data.StepParameters;
using ReplicatorShared.Data.Steps;
using SystemTools.BackgroundTasks;
using SystemTools.SharedKernel;
using SystemTools.SystemToolsShared;
using ToolsManagement.DatabasesManagement;
using ToolsManagement.FileManagersMain;
using WebAgentContracts.WebAgentDatabasesApiContracts.V1.Responses;
using Xunit;

namespace ReplicatorShared.Data.Tests.StepCommands;

public sealed class DatabaseBackupStepCommandTests : IDisposable
{
    private const string FoldersSetName = "HDD";
    private const string DateMask = "yyyyMMddHHmmss";
    private const string BackupNamePrefix = "Pz_";
    private const string BackupNameMiddlePart = "_Full_";
    private const string BackupSuffix = ".bak";
    private const int ProcLineId = 1;
    private const string FirstDatabase = "Db1";
    private const string SecondDatabase = "Db2";

    private static readonly Error BackupError = Error.Problem("BackupError", "Backup failed on server");

    private static readonly Error
        DatabaseListError = Error.Problem("DatabaseListError", "Database list is unavailable");

    private static readonly SmartSchema KeepAllSchema = new()
    {
        LastPreserveCount = 1000,
        Details = [new SmartSchemaDetail { PeriodType = EPeriodType.Day, PreserveCount = 1000 }]
    };

    private static readonly SmartSchema KeepLatestSchema = new()
    {
        LastPreserveCount = 1, Details = [new SmartSchemaDetail { PeriodType = EPeriodType.Day, PreserveCount = 1 }]
    };

    private readonly Dictionary<string, string> _createdBackups = [];
    private readonly Mock<IDatabaseManager> _databaseManager = new();
    private readonly string _localPath;
    private readonly Mock<ILogger> _logger = new();
    private readonly ProcessManager _processManager;
    private readonly string _rootFolder;
    private readonly string _serverBackupFolder;
    private readonly string _uploadFolder;

    private EBackupType _backupType = EBackupType.Full;
    private CompressParameters? _compressParameters;
    private List<string> _databaseNames = [];
    private EDatabaseSet _databaseSet = EDatabaseSet.AllDatabases;
    private string _downloadFileManagerFolder;
    private string? _downloadFileStoragePath;
    private SmartSchema _downloadSideSchema = KeepAllSchema;
    private TimeSpan _holeEndTime = new(23, 59, 59);

    public DatabaseBackupStepCommandTests()
    {
        _rootFolder = Directory.CreateTempSubdirectory("DatabaseBackupStepCommandTests_").FullName;
        _serverBackupFolder = Directory.CreateDirectory(Path.Combine(_rootFolder, "server")).FullName;
        _localPath = Directory.CreateDirectory(Path.Combine(_rootFolder, "local")).FullName;
        _uploadFolder = Directory.CreateDirectory(Path.Combine(_rootFolder, "upload")).FullName;
        _downloadFileManagerFolder = _serverBackupFolder;
        _downloadFileStoragePath = _serverBackupFolder;
        _logger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        _processManager = new ProcessManager(_logger.Object);
    }

    public void Dispose()
    {
        _processManager.Dispose();
        Directory.Delete(_rootFolder, true);
    }

    // ---------- Database selection ----------

    [Theory]
    [InlineData(EBackupType.Full, EDatabaseSet.AllDatabases, "master", "model", "UserFull", "UserSimple")]
    [InlineData(EBackupType.Full, EDatabaseSet.SystemDatabases, "master", "model")]
    [InlineData(EBackupType.Full, EDatabaseSet.AllUserDatabases, "UserFull", "UserSimple")]
    [InlineData(EBackupType.TrLog, EDatabaseSet.AllDatabases, "model", "UserFull")]
    public async Task Run_BacksUpDatabasesOfConfiguredSet(EBackupType backupType, EDatabaseSet databaseSet,
        params string[] expectedDatabases)
    {
        // Arrange
        _backupType = backupType;
        _databaseSet = databaseSet;
        SetupDatabases(ServerDatabases());
        SetupCreateBackupFailure();

        // Act
        bool result = await CreateSut().Run(CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.Equal(expectedDatabases, GetBackedUpDatabases());
    }

    [Fact]
    public async Task Run_WithDatabasesBySelection_BacksUpSelectedDatabasesAndWarnsAboutMissingOnes()
    {
        // Arrange
        _databaseSet = EDatabaseSet.DatabasesBySelection;
        _databaseNames = ["UserSimple", "Missing"];
        SetupDatabases(ServerDatabases());
        SetupCreateBackupFailure();

        // Act
        bool result = await CreateSut().Run(CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.Equal("UserSimple", Assert.Single(GetBackedUpDatabases()));
        VerifyLogged(LogLevel.Warning, "Database with name Missing is missing", Times.Once());
        VerifyLogged(LogLevel.Warning, "Database with name UserSimple is missing", Times.Never());
    }

    [Fact]
    public async Task Run_WithUnknownDatabaseSet_FailsWithoutBackups()
    {
        // Arrange
        _databaseSet = (EDatabaseSet)99;
        SetupDatabases(ServerDatabases());

        // Act
        bool result = await CreateSut().Run(CancellationToken.None);

        // Assert
        Assert.False(result);
        Assert.Empty(GetBackedUpDatabases());
        //Log is only matched here by Moq, nothing is logged
#pragma warning disable CA1873
        _logger.Verify(
            l => l.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
                It.Is<Exception?>(e => e is ArgumentOutOfRangeException &&
                                       e.Message.StartsWith("Invalid database set value.", StringComparison.Ordinal)),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
#pragma warning restore CA1873
    }

    [Fact]
    public async Task Run_WhenDatabaseListCannotBeRead_PrintsErrorAndFails()
    {
        // Arrange
        _databaseManager.Setup(m => m.GetDatabaseNames(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<List<DatabaseInfoModel>>(DatabaseListError));

        // Act
        (bool result, string output) = await RunCapturingConsole(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Contains(DatabaseListError.Description, output, StringComparison.Ordinal);
        Assert.Empty(GetBackedUpDatabases());
    }

    // ---------- Local folder ----------

    [Fact]
    public async Task Run_WhenLocalFolderDoesNotExist_CreatesIt()
    {
        // Arrange
        SetupDatabases();
        DatabaseBackupStepCommand sut = CreateSut();
        Directory.Delete(_localPath);

        // Act
        bool result = await sut.Run(CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.True(Directory.Exists(_localPath));
        VerifyLogged(LogLevel.Warning, $"Local folder {_localPath} does not exist", Times.Once());
        VerifyLogged(LogLevel.Information, $"Creating local folder {_localPath}", Times.Once());
    }

    // ---------- Current period check ----------

    [Fact]
    public async Task Run_WhenBackupForCurrentPeriodExists_SkipsThatDatabase()
    {
        // Arrange
        SetupDatabases(UserDatabase(FirstDatabase), UserDatabase(SecondDatabase));
        CreateFile(_localPath, BackupFileName(FirstDatabase, DateTime.Now));
        CreateFile(_localPath, BackupFileName(SecondDatabase, DateTime.Now) + ".zip");
        SetupCreateBackupFailure();

        // Act
        bool result = await CreateSut().Run(CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.Equal(SecondDatabase, Assert.Single(GetBackedUpDatabases()));
    }

    [Fact]
    public async Task Run_WithArchiver_TreatsArchivedBackupAsCurrentPeriodBackup()
    {
        // Arrange
        _compressParameters = CompressParameters.Create(_logger.Object, false, _localPath, ".go!",
            new ArchiverData { Type = EArchiveType.ZipClass, FileExtension = ".zip" });
        SetupDatabases(UserDatabase(FirstDatabase), UserDatabase(SecondDatabase));
        CreateFile(_localPath, BackupFileName(FirstDatabase, DateTime.Now) + ".zip");
        CreateFile(_localPath, BackupFileName(SecondDatabase, DateTime.Now));
        SetupCreateBackupFailure();

        // Act
        bool result = await CreateSut().Run(CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.Equal(SecondDatabase, Assert.Single(GetBackedUpDatabases()));
    }

    [Fact]
    public async Task Run_OutsideAllowedTimeOfDay_SkipsAllDatabases()
    {
        // Arrange
        _holeEndTime = TimeSpan.Zero;
        SetupDatabases(UserDatabase(FirstDatabase));
        SetupCreateBackupFailure();

        // Act
        bool result = await CreateSut().Run(CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.Empty(GetBackedUpDatabases());
    }

    // ---------- Backup creation ----------

    [Fact]
    public async Task Run_WhenBackupOfOneDatabaseFails_PrintsErrorAndContinuesWithNextDatabase()
    {
        // Arrange
        SetupDatabases(UserDatabase(FirstDatabase), UserDatabase(SecondDatabase));
        SetupCreateBackupFailure();
        SetupCreateBackupInFolder(SecondDatabase, _serverBackupFolder);

        // Act
        (bool result, string output) = await RunCapturingConsole(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Contains(BackupError.Description, output, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(_localPath, _createdBackups[SecondDatabase])));
    }

    [Fact]
    public async Task Run_WhenBackupIsNotInFileStorage_LogsErrorAndSkipsFurtherProcessing()
    {
        // Arrange
        string otherFolder = Directory.CreateDirectory(Path.Combine(_rootFolder, "other")).FullName;
        _downloadSideSchema = KeepLatestSchema;
        string oldBackup = CreateFile(_serverBackupFolder, BackupFileName(FirstDatabase, DateTime.Now.AddDays(-10)));
        string olderBackup = CreateFile(_serverBackupFolder, BackupFileName(FirstDatabase, DateTime.Now.AddDays(-11)));
        SetupDatabases(UserDatabase(FirstDatabase));
        SetupCreateBackupInFolder(FirstDatabase, otherFolder);

        // Act
        bool result = await CreateSut().Run(CancellationToken.None);

        // Assert
        Assert.True(result);
        VerifyLogged(LogLevel.Error,
            $"Backup file {_createdBackups[FirstDatabase]} was created in {otherFolder}, but it is not found in " +
            $"file storage {_serverBackupFolder}. FileStorage must point to backup folder of DbServerFoldersSet HDD",
            Times.Once());
        //file storage was not cleaned, so the older backup is still there
        Assert.True(File.Exists(oldBackup));
        Assert.True(File.Exists(olderBackup));
        Assert.Empty(Directory.GetFiles(_localPath));
    }

    [Fact]
    public async Task Run_WhenBackupCreated_RemovesRedundantServerSideBackups()
    {
        // Arrange
        _downloadSideSchema = KeepLatestSchema;
        string oldBackup = CreateFile(_serverBackupFolder, BackupFileName(FirstDatabase, DateTime.Now.AddDays(-10)));
        SetupDatabases(UserDatabase(FirstDatabase));
        SetupCreateBackupInFolder(FirstDatabase, _serverBackupFolder);

        // Act
        bool result = await CreateSut().Run(CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.False(File.Exists(oldBackup));
        Assert.True(File.Exists(Path.Combine(_serverBackupFolder, _createdBackups[FirstDatabase])));
    }

    // ---------- Download and upload ----------

    [Fact]
    public async Task Run_WhenFileStorageDiffersFromLocalPath_DownloadsAndUploadsBackup()
    {
        // Arrange
        SetupDatabases(UserDatabase(FirstDatabase));
        SetupCreateBackupInFolder(FirstDatabase, _serverBackupFolder);

        // Act
        bool result = await CreateSut().Run(CancellationToken.None);

        // Assert
        Assert.True(result);
        string fileName = _createdBackups[FirstDatabase];
        Assert.True(File.Exists(Path.Combine(_localPath, fileName)));
        Assert.True(File.Exists(Path.Combine(_uploadFolder, fileName)));
        VerifyLogged(LogLevel.Information, "Database Backup Started...", Times.Once());
        VerifyLogged(LogLevel.Information, "Checking parameters...", Times.Once());
        VerifyLogged(LogLevel.Information, "Backup database Db1...", Times.Once());
        VerifyLogged(LogLevel.Information, "Download Backup Started...", Times.Once());
        VerifyLogged(LogLevel.Warning, $"Local folder {_localPath} does not exist", Times.Never());
    }

    [Fact]
    public async Task Run_WhenFileStorageIsLocalPath_UploadsBackupWithoutDownloading()
    {
        // Arrange
        _downloadFileStoragePath = _localPath;
        _downloadFileManagerFolder = _localPath;
        SetupDatabases(UserDatabase(FirstDatabase));
        SetupCreateBackupInFolder(FirstDatabase, _localPath);

        // Act
        bool result = await CreateSut().Run(CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.True(File.Exists(Path.Combine(_uploadFolder, _createdBackups[FirstDatabase])));
        VerifyLogged(LogLevel.Information, "Download Backup Started...", Times.Never());
        VerifyLogged(LogLevel.Information, "Upload Started...", Times.Once());
    }

    [Fact]
    public async Task Run_WhenDownloadFileStorageIsInvalid_LogsErrorAndFails()
    {
        // Arrange
        _downloadFileStoragePath = string.Empty;
        SetupDatabases(UserDatabase(FirstDatabase));
        SetupCreateBackupInFolder(FirstDatabase, _serverBackupFolder);

        // Act
        bool result = await CreateSut().Run(CancellationToken.None);

        // Assert
        Assert.False(result);
        VerifyLogged(LogLevel.Error, "downloadBackupParameters does not created", Times.Once());
        Assert.Empty(Directory.GetFiles(_localPath));
    }

    // ---------- Cancellation ----------

    [Fact]
    public async Task Run_WhenProcessesAreCancelledBeforeBackup_StopsWithoutBackups()
    {
        // Arrange
        SetupDatabases(UserDatabase(FirstDatabase));
        DatabaseBackupStepCommand sut = CreateSut();
        _processManager.CancelProcesses();

        // Act
        bool result = await sut.Run(CancellationToken.None);

        // Assert
        Assert.False(result);
        Assert.Empty(GetBackedUpDatabases());
    }

    [Fact]
    public async Task Run_WhenProcessesAreCancelledDuringBackup_StopsBeforeDownload()
    {
        // Arrange
        SetupDatabases(UserDatabase(FirstDatabase));
        SetupCreateBackupInFolder(FirstDatabase, _serverBackupFolder, _processManager.CancelProcesses);

        // Act
        bool result = await CreateSut().Run(CancellationToken.None);

        // Assert
        Assert.False(result);
        Assert.Empty(Directory.GetFiles(_localPath));
    }

    // ---------- Default retry strategy ----------

    [Fact]
    public async Task Run_WithDefaultRetryStrategy_CreatesSuccessfulBackupOnlyOnce()
    {
        // Arrange
        SetupDatabases(UserDatabase(FirstDatabase));
        SetupCreateBackupInFolder(FirstDatabase, _serverBackupFolder);
        var sut = new DatabaseBackupStepCommand(false, _logger.Object, _processManager, CreateJobStep(),
            CreateParameters(), ".down!");

        // Act
        bool result = await sut.Run(CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.Equal(FirstDatabase, Assert.Single(GetBackedUpDatabases()));
    }

    // ---------- NeedDownload ----------

    [Fact]
    public void NeedDownload_WhenFileStoragePathIsNotSpecified_ReturnsFalse()
    {
        // Arrange
        _downloadFileStoragePath = " ";

        // Act & Assert
        Assert.False(CreateSut().NeedDownload());
    }

    [Fact]
    public void NeedDownload_WhenFileStorageIsRemote_ReturnsTrue()
    {
        // Arrange
        _downloadFileStoragePath = "ftp://example.invalid/backups";

        // Act & Assert
        Assert.True(CreateSut().NeedDownload());
    }

    [Fact]
    public void NeedDownload_WhenFileStorageIsLocalPath_ReturnsFalse()
    {
        // Arrange
        _downloadFileStoragePath = _localPath + Path.DirectorySeparatorChar;

        // Act & Assert
        Assert.False(CreateSut().NeedDownload());
    }

    [Fact]
    public void NeedDownload_WhenFileStorageIsAnotherLocalFolder_ReturnsTrue()
    {
        // Act & Assert
        Assert.True(CreateSut().NeedDownload());
    }

    // ---------- Helpers ----------

    private DatabaseBackupStepCommand CreateSut()
    {
        return new DatabaseBackupStepCommand(false, _logger.Object, _processManager, CreateJobStep(),
            CreateParameters(), ".down!", null, ResiliencePipeline<Result<BackupFileParameters>>.Empty);
    }

    private DatabaseBackupStep CreateJobStep()
    {
        return new DatabaseBackupStep
        {
            ProcLineId = ProcLineId,
            PeriodType = EPeriodType.Day,
            StartAt = DateTime.Today.AddDays(-10),
            HoleStartTime = TimeSpan.Zero,
            HoleEndTime = _holeEndTime
        };
    }

    private DatabaseBackupStepParameters CreateParameters()
    {
        var downloadFileStorage = new FileStorageData { FileStoragePath = _downloadFileStoragePath };
        var uploadFileStorage = new FileStorageData { FileStoragePath = _uploadFolder };
        UploadParameters uploadParameters =
            UploadParameters.Create(_logger.Object, false, _localPath, uploadFileStorage, KeepAllSchema, ".up!",
                ProcLineId) ?? throw new InvalidOperationException("Upload parameters were not created");
        var dbBackupParameters = new DatabaseBackupParametersDomain(BackupNamePrefix, DateMask, BackupSuffix,
            BackupNameMiddlePart, false, false, _backupType);

        return new DatabaseBackupStepParameters(_databaseManager.Object, _localPath, CreateFileManager(_localPath),
            _backupType, _databaseSet, _databaseNames, downloadFileStorage, uploadFileStorage, _downloadSideSchema,
            KeepAllSchema, CreateFileManager(_downloadFileManagerFolder), dbBackupParameters, ProcLineId, ProcLineId,
            _compressParameters, uploadParameters, FoldersSetName);
    }

    private FileManager CreateFileManager(string folder)
    {
        return FileManagersFactory.CreateFileManager(false, _logger.Object, folder) ??
               throw new InvalidOperationException($"File manager for {folder} was not created");
    }

    private static DatabaseInfoModel[] ServerDatabases()
    {
        return
        [
            new DatabaseInfoModel("master", EDatabaseRecoveryModel.Simple, true),
            new DatabaseInfoModel("model", EDatabaseRecoveryModel.Full, true),
            new DatabaseInfoModel("UserFull", EDatabaseRecoveryModel.Full, false),
            new DatabaseInfoModel("UserSimple", EDatabaseRecoveryModel.Simple, false)
        ];
    }

    private static DatabaseInfoModel UserDatabase(string databaseName)
    {
        return new DatabaseInfoModel(databaseName, EDatabaseRecoveryModel.Full, false);
    }

    private static string BackupFileName(string databaseName, DateTime date)
    {
        return BackupNamePrefix + databaseName + BackupNameMiddlePart +
               date.ToString(DateMask, CultureInfo.InvariantCulture) + BackupSuffix;
    }

    private static string CreateFile(string folder, string fileName)
    {
        string filePath = Path.Combine(folder, fileName);
        File.WriteAllText(filePath, fileName);
        return filePath;
    }

    private void SetupDatabases(params DatabaseInfoModel[] databases)
    {
        List<DatabaseInfoModel> databaseInfos = [.. databases];
        _databaseManager.Setup(m => m.GetDatabaseNames(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(databaseInfos));
    }

    private void SetupCreateBackupFailure()
    {
        _databaseManager.Setup(m => m.CreateBackup(It.IsAny<DatabaseBackupParametersDomain>(), It.IsAny<string>(),
                FoldersSetName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<BackupFileParameters>(BackupError));
    }

    private void SetupCreateBackupInFolder(string databaseName, string folder)
    {
        SetupCreateBackupInFolder(databaseName, folder, static () => { });
    }

    private void SetupCreateBackupInFolder(string databaseName, string folder, Action onBackup)
    {
        _databaseManager.Setup(m => m.CreateBackup(It.IsAny<DatabaseBackupParametersDomain>(), databaseName,
            FoldersSetName, It.IsAny<CancellationToken>())).ReturnsAsync(() =>
        {
            onBackup();
            string fileName = BackupFileName(databaseName, DateTime.Now);
            CreateFile(folder, fileName);
            _createdBackups[databaseName] = fileName;
            return Result.Success(new BackupFileParameters(folder, fileName,
                BackupNamePrefix + databaseName + BackupNameMiddlePart, BackupSuffix, DateMask));
        });
    }

    private List<string> GetBackedUpDatabases()
    {
        return
        [
            .. _databaseManager.Invocations.Where(i => i.Method.Name == nameof(IDatabaseManager.CreateBackup))
                .Select(i => (string)i.Arguments[1]).Where(name => !string.IsNullOrEmpty(name)).Cast<string>()
        ];
    }

    private static async Task<(bool Result, string Output)> RunCapturingConsole(DatabaseBackupStepCommand sut)
    {
        TextWriter originalOutput = Console.Out;
        await using var output = new StringWriter(CultureInfo.InvariantCulture);
        Console.SetOut(output);
        try
        {
            bool result = await sut.Run(CancellationToken.None);
            return (result, output.ToString());
        }
        finally
        {
            Console.SetOut(originalOutput);
        }
    }

    private void VerifyLogged(LogLevel logLevel, string message, Times times)
    {
        //Log is only matched here by Moq, nothing is logged
#pragma warning disable CA1873
        _logger.Verify(
            l => l.Log(logLevel, It.IsAny<EventId>(), It.Is<It.IsAnyType>((state, _) => state!.ToString() == message),
                It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), times);
#pragma warning restore CA1873
    }
}
