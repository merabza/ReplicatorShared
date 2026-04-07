using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DatabaseTools.DbTools;
using DatabaseTools.DbTools.Models;
using OneOf;
using ReplicatorShared.Data.Models;
using SystemTools.SystemToolsShared.Errors;
using ToolsManagement.DatabasesManagement;

namespace ReplicatorShared.Data;

public sealed class DatabasesListCreator
{
    //private readonly bool _byParameters;
    private readonly IDatabaseManager _agentClient;

    private readonly EBackupType? _backupType;
    private readonly EDatabaseSet _databaseSet;

    // ReSharper disable once ConvertToPrimaryConstructor
    public DatabasesListCreator(EDatabaseSet databaseSet, IDatabaseManager agentClient, EBackupType? backupType = null)
    {
        _databaseSet = databaseSet;
        _agentClient = agentClient;
        _backupType = backupType;
    }

    public async Task<List<DatabaseInfoModel>> LoadDatabaseNames(CancellationToken cancellationToken = default)
    {
        OneOf<List<DatabaseInfoModel>, Error[]> getDatabaseNamesResult =
            await _agentClient.GetDatabaseNames(cancellationToken);
        List<DatabaseInfoModel>? databaseInfos = getDatabaseNamesResult.AsT0;

        (bool sysBaseDoesMatter, bool checkSysBase) = GetDbSetParams(_databaseSet);

        return databaseInfos.Where(w =>
            (!sysBaseDoesMatter || w.IsSystemDatabase == checkSysBase) &&
            (w.RecoveryModel != EDatabaseRecoveryModel.Simple || _backupType != EBackupType.TrLog)).ToList();
    }

    private static (bool, bool) GetDbSetParams(EDatabaseSet databaseSet)
    {
        bool sysBaseDoesMatter = false;
        bool checkSysBase = false;

        switch (databaseSet)
        {
            case EDatabaseSet.SystemDatabases:
                checkSysBase = true;
                sysBaseDoesMatter = true;
                break;
            case EDatabaseSet.AllUserDatabases:
                sysBaseDoesMatter = true;
                break;
            case EDatabaseSet.AllDatabases:
            case EDatabaseSet.DatabasesBySelection:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(databaseSet), databaseSet,
                    "Unexpected database set value");
        }

        return (sysBaseDoesMatter, checkSysBase);
    }
}
