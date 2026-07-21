// #region -->Child ETL for pl_notes_samms_to_lakehouse

{
    "name": "pl_note_saams_to_lakehouse",
    "objectId": "61f2955b-68c9-4a3b-8da7-186a0ce8e23e",
    "properties": {
        "activities": [
            {
                "name": "flt_child_arnote_sites",
                "type": "Filter",
                "dependsOn": [],
                "typeProperties": {
                    "items": {
                        "value": "@pipeline().parameters.p_sites",
                        "type": "Expression"
                    },
                    "condition": {
                        "value": "@equals(item().Method, '3pArnote')",
                        "type": "Expression"
                    }
                }
            },
            {
                "name": "flt_child_claimnote_sites",
                "type": "Filter",
                "dependsOn": [],
                "typeProperties": {
                    "items": {
                        "value": "@pipeline().parameters.p_sites",
                        "type": "Expression"
                    },
                    "condition": {
                        "value": "@equals(item().Method, '3pClaimNote')",
                        "type": "Expression"
                    }
                }
            },
            {
                "name": "fe_each_samms_site_arnote",
                "type": "ForEach",
                "dependsOn": [
                    {
                        "activity": "flt_child_arnote_sites",
                        "dependencyConditions": [
                            "Succeeded"
                        ]
                    }
                ],
                "typeProperties": {
                    "items": {
                        "value": "@activity('flt_child_arnote_sites').output.value",
                        "type": "Expression"
                    },
                    "isSequential": false,
                    "batchCount": 5,
                    "activities": [
                        {
                            "name": "cp_arnote_to_bronze",
                            "type": "Copy",
                            "dependsOn": [
                                {
                                    "activity": "lkp_check_arnote_globalbatchid_exists",
                                    "dependencyConditions": [
                                        "Succeeded"
                                    ]
                                }
                            ],
                            "policy": {
                                "timeout": "0.12:00:00",
                                "retry": 0,
                                "retryIntervalInSeconds": 30,
                                "secureOutput": false,
                                "secureInput": false
                            },
                            "typeProperties": {
                                "source": {
                                    "type": "SqlServerSource",
                                    "sqlReaderQuery": {
                                        "value": "@concat(\n'SELECT\n    ''', item().SiteCode, ''' AS _site_code,\n    ''', item().DataBaseName, ''' AS _source_database,\n    ''', pipeline().parameters.p_ingest_run_id, ''' AS _ingest_run_id,\n    GETDATE() AS _extracted_at,\n    CONVERT(date, DATEADD(day, -', string(coalesce(item().LookbackDays, pipeline().parameters.p_lookback_days)), ', CONVERT(date, ''', pipeline().parameters.p_work_date, '''))) AS _source_query_date_anchor,\n\n    arnID,\n    arnLIID,\n    arnNOTE,\n    arnUSER,\n    arnDATE,\n    arnDtRemoved,\n    arnStrRemovedReason,\n    arnStrRemovedUser,\n    bid,\n    arnDBnotes,\n    ', if(equals(activity('lkp_check_arnote_globalbatchid_exists').output.value[0].globalbatchid_exists, 1), 'globalBatchId,', 'CAST(NULL AS bigint) AS globalBatchId,'), '\n\n    RowChkSum = CHECKSUM(\n        arnID,\n        arnLIID,\n        arnNOTE,\n        arnUSER,\n        arnDATE,\n        arnDtRemoved,\n        arnStrRemovedReason,\n        arnStrRemovedUser,\n        bid,\n        arnDBnotes,\n        ', if(equals(activity('lkp_check_arnote_globalbatchid_exists').output.value[0].globalbatchid_exists, 1), 'globalBatchId', 'CAST(NULL AS bigint)'), '\n    )\n\nFROM [', item().DataBaseName, '].dbo.[', item().SourceTable, ']\nWHERE (arnDATE >= ''2023-01-01'' AND arnDATE >= CONVERT(date, DATEADD(day, -', string(coalesce(item().LookbackDays, pipeline().parameters.p_lookback_days)), ', CONVERT(date, ''', pipeline().parameters.p_work_date, '''))))\n   OR arnDtRemoved >= CONVERT(date, DATEADD(day, -', string(coalesce(item().LookbackDays, pipeline().parameters.p_lookback_days)), ', CONVERT(date, ''', pipeline().parameters.p_work_date, ''')))\nUNION ALL\nSELECT\n    ''', item().SiteCode, ''' AS _site_code,\n    ''', item().DataBaseName, ''' AS _source_database,\n    ''', pipeline().parameters.p_ingest_run_id, ''' AS _ingest_run_id,\n    GETDATE() AS _extracted_at,\n    CONVERT(date, DATEADD(day, -', string(coalesce(item().LookbackDays, pipeline().parameters.p_lookback_days)), ', CONVERT(date, ''', pipeline().parameters.p_work_date, '''))) AS _source_query_date_anchor,\n    CAST(NULL AS int) AS arnID,\n    CAST(NULL AS int) AS arnLIID,\n    CAST(NULL AS varchar(max)) AS arnNOTE,\n    CAST(NULL AS varchar(50)) AS arnUSER,\n    CAST(NULL AS datetime) AS arnDATE,\n    CAST(NULL AS datetime) AS arnDtRemoved,\n    CAST(NULL AS varchar(max)) AS arnStrRemovedReason,\n    CAST(NULL AS varchar(100)) AS arnStrRemovedUser,\n    CAST(NULL AS int) AS bid,\n    CAST(NULL AS varchar(250)) AS arnDBnotes,\n    CAST(NULL AS bigint) AS globalBatchId,\n    CAST(NULL AS int) AS RowChkSum\nORDER BY arnID'\n)",
                                        "type": "Expression"
                                    },
                                    "queryTimeout": "02:00:00",
                                    "partitionOption": "None",
                                    "datasetSettings": {
                                        "annotations": [],
                                        "type": "SqlServerTable",
                                        "schema": [],
                                        "externalReferences": {
                                            "connection": "9743b95a-fd66-4f7c-9767-e6eb0f1ecab7"
                                        }
                                    }
                                },
                                "sink": {
                                    "type": "LakehouseTableSink",
                                    "tableActionOption": "Append",
                                    "partitionOption": "None",
                                    "applyVOrder": false,
                                    "datasetSettings": {
                                        "annotations": [],
                                        "linkedService": {
                                            "name": "bhg_bronze",
                                            "properties": {
                                                "annotations": [],
                                                "type": "Lakehouse",
                                                "typeProperties": {
                                                    "workspaceId": "c5097ffb-b78e-441d-9575-a82bac23cac8",
                                                    "artifactId": "77d24027-6a1c-43a8-a998-1a14dd3c0d52",
                                                    "rootFolder": "Tables"
                                                }
                                            }
                                        },
                                        "type": "LakehouseTable",
                                        "schema": [],
                                        "typeProperties": {
                                            "schema": "Notes",
                                            "table": "br_tbl3pArnote"
                                        }
                                    }
                                },
                                "enableStaging": false,
                                "translator": {
                                    "type": "TabularTranslator",
                                    "typeConversion": true,
                                    "typeConversionSettings": {
                                        "allowDataTruncation": true,
                                        "treatBooleanAsNumber": false
                                    }
                                }
                            }
                        },
                        {
                            "name": "lkp_check_arnote_globalbatchid_exists",
                            "type": "Lookup",
                            "dependsOn": [],
                            "policy": {
                                "timeout": "0.12:00:00",
                                "retry": 0,
                                "retryIntervalInSeconds": 30,
                                "secureOutput": false,
                                "secureInput": false
                            },
                            "typeProperties": {
                                "source": {
                                    "type": "SqlServerSource",
                                    "sqlReaderQuery": {
                                        "value": "@concat(\n'SELECT globalbatchid_exists = COUNT(1)\nFROM [', item().DataBaseName, '].sys.columns c\nINNER JOIN [', item().DataBaseName, '].sys.tables t ON c.object_id = t.object_id\nINNER JOIN [', item().DataBaseName, '].sys.schemas s ON t.schema_id = s.schema_id\nWHERE s.name = ''dbo''\n  AND t.name = ''', item().SourceTable, '''\n  AND c.name = ''globalBatchId'''\n)",
                                        "type": "Expression"
                                    },
                                    "queryTimeout": "02:00:00",
                                    "partitionOption": "None"
                                },
                                "firstRowOnly": false,
                                "datasetSettings": {
                                    "annotations": [],
                                    "type": "SqlServerTable",
                                    "schema": [],
                                    "externalReferences": {
                                        "connection": "9743b95a-fd66-4f7c-9767-e6eb0f1ecab7"
                                    }
                                }
                            }
                        }
                    ]
                }
            },
            {
                "name": "fe_each_samms_site_claimnote",
                "type": "ForEach",
                "dependsOn": [
                    {
                        "activity": "flt_child_claimnote_sites",
                        "dependencyConditions": [
                            "Succeeded"
                        ]
                    }
                ],
                "typeProperties": {
                    "items": {
                        "value": "@activity('flt_child_claimnote_sites').output.value",
                        "type": "Expression"
                    },
                    "isSequential": false,
                    "batchCount": 5,
                    "activities": [
                        {
                            "name": "cp_claimnote_to_bronze",
                            "type": "Copy",
                            "dependsOn": [
                                {
                                    "activity": "lkp_check_claimnote_globalbatchid_exists",
                                    "dependencyConditions": [
                                        "Succeeded"
                                    ]
                                }
                            ],
                            "policy": {
                                "timeout": "0.12:00:00",
                                "retry": 0,
                                "retryIntervalInSeconds": 30,
                                "secureOutput": false,
                                "secureInput": false
                            },
                            "typeProperties": {
                                "source": {
                                    "type": "SqlServerSource",
                                    "sqlReaderQuery": {
                                        "value": "@concat(\n 'SELECT\n    ''', item().SiteCode, ''' AS _site_code,\n    ''', item().DataBaseName, ''' AS _source_database,\n    ''', pipeline().parameters.p_ingest_run_id, ''' AS _ingest_run_id,\n    GETDATE() AS _extracted_at,\n    CONVERT(date, DATEADD(day, -', string(coalesce(item().LookbackDays, pipeline().parameters.p_lookback_days)), ', CONVERT(date, ''', pipeline().parameters.p_work_date, '''))) AS _source_query_date_anchor,\n\n    tpcn,\n    tpcnTPCID,\n    tpcnDtmAdded,\n    tpcnStrAdded,\n    tpcnStrNote,\n    tpcnStrType,\n    tpcnDtTickler,\n    tpcnDtTicklerRemoved,\n    tpcnStrTicklerRemovedNote,\n    tpcnStrTicklerRemovedUser,\n    tpcnStrTicklerType,\n    ', if(equals(activity('lkp_check_claimnote_globalbatchid_exists').output.value[0].globalbatchid_exists, 1), 'globalBatchId,', 'CAST(NULL AS bigint) AS globalBatchId,'), '\n\n    RowChkSum = CHECKSUM(\n        tpcn,\n        tpcnTPCID,\n        tpcnDtmAdded,\n        tpcnStrAdded,\n        tpcnStrNote,\n        tpcnStrType,\n        tpcnDtTickler,\n        tpcnDtTicklerRemoved,\n        tpcnStrTicklerRemovedNote,\n        tpcnStrTicklerRemovedUser,\n        tpcnStrTicklerType,\n        ', if(equals(activity('lkp_check_claimnote_globalbatchid_exists').output.value[0].globalbatchid_exists, 1), 'globalBatchId', 'CAST(NULL AS bigint)'), '\n    )\n\nFROM [', item().DataBaseName, '].dbo.[', item().SourceTable, ']\nWHERE (tpcnDtmAdded >= ''2023-01-01'' AND tpcnDtmAdded >= CONVERT(date, DATEADD(day, -', string(coalesce(item().LookbackDays, pipeline().parameters.p_lookback_days)), ', CONVERT(date, ''', pipeline().parameters.p_work_date, '''))))\n   OR tpcnDtTickler >= CONVERT(date, DATEADD(day, -', string(coalesce(item().LookbackDays, pipeline().parameters.p_lookback_days)), ', CONVERT(date, ''', pipeline().parameters.p_work_date, ''')))\nUNION ALL\nSELECT\n    ''', item().SiteCode, ''' AS _site_code,\n    ''', item().DataBaseName, ''' AS _source_database,\n    ''', pipeline().parameters.p_ingest_run_id, ''' AS _ingest_run_id,\n    GETDATE() AS _extracted_at,\n    CONVERT(date, DATEADD(day, -', string(coalesce(item().LookbackDays, pipeline().parameters.p_lookback_days)), ', CONVERT(date, ''', pipeline().parameters.p_work_date, '''))) AS _source_query_date_anchor,\n    CAST(NULL AS int) AS tpcn,\n    CAST(NULL AS int) AS tpcnTPCID,\n    CAST(NULL AS datetime) AS tpcnDtmAdded,\n    CAST(NULL AS varchar(100)) AS tpcnStrAdded,\n    CAST(NULL AS varchar(1000)) AS tpcnStrNote,\n    CAST(NULL AS varchar(10)) AS tpcnStrType,\n    CAST(NULL AS datetime) AS tpcnDtTickler,\n    CAST(NULL AS varchar(max)) AS tpcnDtTicklerRemoved,\n    CAST(NULL AS varchar(max)) AS tpcnStrTicklerRemovedNote,\n    CAST(NULL AS varchar(100)) AS tpcnStrTicklerRemovedUser,\n    CAST(NULL AS varchar(500)) AS tpcnStrTicklerType,\n    CAST(NULL AS bigint) AS globalBatchId,\n    CAST(NULL AS int) AS RowChkSum\nORDER BY tpcn'\n)",
                                        "type": "Expression"
                                    },
                                    "queryTimeout": "02:00:00",
                                    "partitionOption": "None",
                                    "datasetSettings": {
                                        "annotations": [],
                                        "type": "SqlServerTable",
                                        "schema": [],
                                        "externalReferences": {
                                            "connection": "9743b95a-fd66-4f7c-9767-e6eb0f1ecab7"
                                        }
                                    }
                                },
                                "sink": {
                                    "type": "LakehouseTableSink",
                                    "tableActionOption": "Append",
                                    "partitionOption": "None",
                                    "applyVOrder": false,
                                    "datasetSettings": {
                                        "annotations": [],
                                        "linkedService": {
                                            "name": "bhg_bronze",
                                            "properties": {
                                                "annotations": [],
                                                "type": "Lakehouse",
                                                "typeProperties": {
                                                    "workspaceId": "c5097ffb-b78e-441d-9575-a82bac23cac8",
                                                    "artifactId": "77d24027-6a1c-43a8-a998-1a14dd3c0d52",
                                                    "rootFolder": "Tables"
                                                }
                                            }
                                        },
                                        "type": "LakehouseTable",
                                        "schema": [],
                                        "typeProperties": {
                                            "schema": "Notes",
                                            "table": "br_tbl3pClaimNote"
                                        }
                                    }
                                },
                                "enableStaging": false,
                                "translator": {
                                    "type": "TabularTranslator",
                                    "typeConversion": true,
                                    "typeConversionSettings": {
                                        "allowDataTruncation": true,
                                        "treatBooleanAsNumber": false
                                    }
                                }
                            }
                        },
                        {
                            "name": "lkp_check_claimnote_globalbatchid_exists",
                            "type": "Lookup",
                            "dependsOn": [],
                            "policy": {
                                "timeout": "0.12:00:00",
                                "retry": 0,
                                "retryIntervalInSeconds": 30,
                                "secureOutput": false,
                                "secureInput": false
                            },
                            "typeProperties": {
                                "source": {
                                    "type": "SqlServerSource",
                                    "sqlReaderQuery": {
                                        "value": "@concat(\n'SELECT globalbatchid_exists = COUNT(1)\nFROM [', item().DataBaseName, '].sys.columns c\nINNER JOIN [', item().DataBaseName, '].sys.tables t ON c.object_id = t.object_id\nINNER JOIN [', item().DataBaseName, '].sys.schemas s ON t.schema_id = s.schema_id\nWHERE s.name = ''dbo''\n  AND t.name = ''', item().SourceTable, '''\n  AND c.name = ''globalBatchId'''\n)",
                                        "type": "Expression"
                                    },
                                    "queryTimeout": "02:00:00",
                                    "partitionOption": "None"
                                },
                                "firstRowOnly": false,
                                "datasetSettings": {
                                    "annotations": [],
                                    "type": "SqlServerTable",
                                    "schema": [],
                                    "externalReferences": {
                                        "connection": "9743b95a-fd66-4f7c-9767-e6eb0f1ecab7"
                                    }
                                }
                            }
                        }
                    ]
                }
            },
            {
                "name": "set_child_bronze_method_results",
                "type": "SetVariable",
                "dependsOn": [
                    {
                        "activity": "fe_each_samms_site_arnote",
                        "dependencyConditions": [
                            "Completed"
                        ]
                    },
                    {
                        "activity": "fe_each_samms_site_claimnote",
                        "dependencyConditions": [
                            "Completed"
                        ]
                    }
                ],
                "policy": {
                    "secureOutput": false,
                    "secureInput": false
                },
                "typeProperties": {
                    "variableName": "pipelineReturnValue",
                    "value": [
                        {
                            "key": "v_bronze_method_results_json",
                            "value": {
                                "type": "Expression",
                                "content": "@concat('{','\"3pArnote\":{\"status\":\"',if(equals(activity('fe_each_samms_site_arnote').Status,'Succeeded'),'SUCCESS','FAILED'),'\",\"failed_stage\":\"',if(equals(activity('fe_each_samms_site_arnote').Status,'Succeeded'),'','BR'),'\",\"error_message\":',if(equals(activity('fe_each_samms_site_arnote').Status,'Succeeded'),'null',concat('\"',replace(replace(string(coalesce(activity('fe_each_samms_site_arnote').error,'3pArnote Bronze ForEach failed')),'\"',''''),'\\','\\\\'),'\"')),'},','\"3pClaimNote\":{\"status\":\"',if(equals(activity('fe_each_samms_site_claimnote').Status,'Succeeded'),'SUCCESS','FAILED'),'\",\"failed_stage\":\"',if(equals(activity('fe_each_samms_site_claimnote').Status,'Succeeded'),'','BR'),'\",\"error_message\":',if(equals(activity('fe_each_samms_site_claimnote').Status,'Succeeded'),'null',concat('\"',replace(replace(string(coalesce(activity('fe_each_samms_site_claimnote').error,'3pClaimNote Bronze ForEach failed')),'\"',''''),'\\','\\\\'),'\"')),'}','}')"
                            }
                        }
                    ],
                    "setSystemVariable": true
                }
            }
        ],
        "parameters": {
            "p_ingest_run_id": {
                "type": "string",
                "defaultValue": "test-run-001"
            },
            "p_work_date": {
                "type": "string",
                "defaultValue": "2026-07-14"
            },
            "p_lookback_days": {
                "type": "int",
                "defaultValue": 15
            },
            "p_sites": {
                "type": "array",
                "defaultValue": [
                    {
                        "SiteCode": "AHK",
                        "DataBaseName": "SAMMS-Ahoskie",
                        "Method": "3pArnote",
                        "SourceTable": "tbl3pARNOTE"
                    },
                    {
                        "SiteCode": "AHK",
                        "DataBaseName": "SAMMS-Ahoskie",
                        "Method": "3pClaimNote",
                        "SourceTable": "tbl3pClaimNote"
                    }
                ]
            }
        },
        "variables": {
            "v_bronze_method_results_json": {
                "type": "String"
            }
        },
        "lastModifiedByObjectId": "41032ad8-8248-4dd3-9ac8-0281d6ef4ebd",
        "lastPublishTime": "2026-07-16T13:39:04Z"
    }
}


// #endregion -->Child ETL for pl_notes_samms_to_lakehouse


// #region -->Parent ETL Execute_Notes

{
    "name": "pl_execute_notes",
    "objectId": "189b74e5-ef11-4ee4-afb8-4a9b8a576f30",
    "properties": {
        "activities": [
            {
                "name": "lkp_notes_taskconfig",
                "type": "Lookup",
                "state": "Inactive",
                "onInactiveMarkAs": "Succeeded",
                "dependsOn": [],
                "policy": {
                    "timeout": "0.12:00:00",
                    "retry": 0,
                    "retryIntervalInSeconds": 30,
                    "secureOutput": false,
                    "secureInput": false
                },
                "typeProperties": {
                    "source": {
                        "type": "LakehouseTableSource"
                    },
                    "firstRowOnly": false,
                    "datasetSettings": {
                        "annotations": [],
                        "linkedService": {
                            "name": "bhg_bronze",
                            "properties": {
                                "annotations": [],
                                "type": "Lakehouse",
                                "typeProperties": {
                                    "workspaceId": "c5097ffb-b78e-441d-9575-a82bac23cac8",
                                    "artifactId": "77d24027-6a1c-43a8-a998-1a14dd3c0d52",
                                    "rootFolder": "Tables"
                                }
                            }
                        },
                        "type": "LakehouseTable",
                        "schema": [],
                        "typeProperties": {
                            "schema": "meta",
                            "table": "taskconfig"
                        }
                    }
                }
            },
            {
                "name": "flt_active_notes_sites",
                "type": "Filter",
                "dependsOn": [
                    {
                        "activity": "nb_get_notes_taskconfig",
                        "dependencyConditions": [
                            "Succeeded"
                        ]
                    }
                ],
                "typeProperties": {
                    "items": {
                        "value": "@json(activity('nb_get_notes_taskconfig').output.result.exitValue)",
                        "type": "Expression"
                    },
                    "condition": {
                        "value": "@and(and(equals(item().ConfigId, 34), equals(item().IsActive, 1)), and(or(equals(item().Method, '3pArnote'), equals(item().Method, '3pClaimNote')), and(not(equals(item().SiteCode, null)), and(not(equals(item().DataBaseName, null)), not(equals(item().SourceTable, null))))))",
                        "type": "Expression"
                    }
                }
            },
            {
                "name": "nb_notes_audit_start",
                "type": "TridentNotebook",
                "dependsOn": [
                    {
                        "activity": "flt_active_notes_sites",
                        "dependencyConditions": [
                            "Succeeded"
                        ]
                    }
                ],
                "policy": {
                    "timeout": "0.12:00:00",
                    "retry": 0,
                    "retryIntervalInSeconds": 30,
                    "secureOutput": false,
                    "secureInput": false
                },
                "typeProperties": {
                    "notebookId": "9d0b3480-fa72-4814-ad31-7bbec83a3301",
                    "workspaceId": "c5097ffb-b78e-441d-9575-a82bac23cac8",
                    "parameters": {
                        "p_mode": {
                            "value": "START_LAYER_RUNS",
                            "type": "string"
                        },
                        "p_config_name_prefix": {
                            "value": "SAMMS Notes",
                            "type": "string"
                        },
                        "p_pipeline_name": {
                            "value": "Execute_Notes",
                            "type": "string"
                        },
                        "p_pipeline_path": {
                            "value": "/pipelines/Execute_Notes",
                            "type": "string"
                        },
                        "p_triggered_by": {
                            "value": "Fabric",
                            "type": "string"
                        }
                    }
                }
            },
            {
                "name": "Executed_AfterBronzz",
                "type": "ExecutePipeline",
                "state": "Inactive",
                "onInactiveMarkAs": "Succeeded",
                "dependsOn": [],
                "policy": {
                    "secureInput": false
                },
                "typeProperties": {
                    "pipeline": {
                        "referenceName": "61f2955b-68c9-4a3b-8da7-186a0ce8e23e",
                        "type": "PipelineReference"
                    },
                    "waitOnCompletion": true,
                    "parameters": {
                        "p_ingest_run_id": {
                            "value": "@pipeline().RunId",
                            "type": "Expression"
                        },
                        "p_work_date": {
                            "value": "@convertFromUtc(utcNow(), 'Central Standard Time', 'yyyy-MM-dd')",
                            "type": "Expression"
                        },
                        "p_lookback_days": {
                            "value": "@pipeline().parameters.p_lookback_days",
                            "type": "Expression"
                        },
                        "p_sites": {
                            "value": "@activity('flt_active_notes_sites').output.value",
                            "type": "Expression"
                        }
                    }
                }
            },
            {
                "name": "set_bronze_method_results_from_child",
                "type": "SetVariable",
                "dependsOn": [
                    {
                        "activity": "Executed_AfterBronz",
                        "dependencyConditions": [
                            "Completed"
                        ]
                    }
                ],
                "policy": {
                    "secureOutput": false,
                    "secureInput": false
                },
                "typeProperties": {
                    "variableName": "v_bronze_method_results_json",
                    "value": {
                        "value": "@if(equals(activity('Executed_AfterBronz').Status,'Succeeded'), string(activity('Executed_AfterBronz').output.properties.returnValue.v_bronze_method_results_json), '{\"3pArnote\":{\"status\":\"FAILED\",\"failed_stage\":\"BR\",\"error_message\":\"Notes Bronze child pipeline failed before returning method results\"},\"3pClaimNote\":{\"status\":\"FAILED\",\"failed_stage\":\"BR\",\"error_message\":\"Notes Bronze child pipeline failed before returning method results\"}}')",
                        "type": "Expression"
                    }
                }
            },
            {
                "name": "nb_3parnote_bronze_to_silver",
                "type": "TridentNotebook",
                "dependsOn": [
                    {
                        "activity": "set_bronze_method_results_from_child",
                        "dependencyConditions": [
                            "Succeeded"
                        ]
                    }
                ],
                "policy": {
                    "timeout": "0.12:00:00",
                    "retry": 0,
                    "retryIntervalInSeconds": 30,
                    "secureOutput": false,
                    "secureInput": false
                },
                "typeProperties": {
                    "notebookId": "4057574c-6a08-4c3e-8584-9ead64ee8608",
                    "workspaceId": "c5097ffb-b78e-441d-9575-a82bac23cac8",
                    "parameters": {
                        "p_ingest_run_id": {
                            "value": {
                                "value": "@pipeline().RunId",
                                "type": "Expression"
                            },
                            "type": "string"
                        },
                        "p_bronze_succeeded": {
                            "value": {
                                "value": "@string(equals(json(variables('v_bronze_method_results_json'))['3pArnote']['status'], 'SUCCESS'))",
                                "type": "Expression"
                            },
                            "type": "string"
                        },
                        "p_bronze_method_results_json": {
                            "value": {
                                "value": "@variables('v_bronze_method_results_json')",
                                "type": "Expression"
                            },
                            "type": "string"
                        },
                        "p_sites_json": {
                            "value": {
                                "value": "@string(activity('flt_active_notes_sites').output.value)",
                                "type": "Expression"
                            },
                            "type": "string"
                        },
                        "p_taskconfig_json": {
                            "value": {
                                "value": "@activity('nb_get_notes_taskconfig').output.result.exitValue",
                                "type": "Expression"
                            },
                            "type": "string"
                        },
                        "p_method": {
                            "value": "3pArnote",
                            "type": "string"
                        },
                        "p_bronze_config_id": {
                            "value": 34,
                            "type": "int"
                        },
                        "p_silver_config_id": {
                            "value": 35,
                            "type": "int"
                        }
                    }
                }
            },
            {
                "name": "nb_3pclaimnote_bronze_to_silver",
                "type": "TridentNotebook",
                "dependsOn": [
                    {
                        "activity": "set_bronze_method_results_from_child",
                        "dependencyConditions": [
                            "Succeeded"
                        ]
                    }
                ],
                "policy": {
                    "timeout": "0.12:00:00",
                    "retry": 0,
                    "retryIntervalInSeconds": 30,
                    "secureOutput": false,
                    "secureInput": false
                },
                "typeProperties": {
                    "notebookId": "ecb28154-151c-46c5-8c67-99940dd9d570",
                    "workspaceId": "c5097ffb-b78e-441d-9575-a82bac23cac8",
                    "parameters": {
                        "p_ingest_run_id": {
                            "value": {
                                "value": "@pipeline().RunId",
                                "type": "Expression"
                            },
                            "type": "string"
                        },
                        "p_bronze_succeeded": {
                            "value": {
                                "value": "@string(equals(json(variables('v_bronze_method_results_json'))['3pClaimNote']['status'], 'SUCCESS'))",
                                "type": "Expression"
                            },
                            "type": "string"
                        },
                        "p_bronze_method_results_json": {
                            "value": {
                                "value": "@variables('v_bronze_method_results_json')",
                                "type": "Expression"
                            },
                            "type": "string"
                        },
                        "p_sites_json": {
                            "value": {
                                "value": "@string(activity('flt_active_notes_sites').output.value)",
                                "type": "Expression"
                            },
                            "type": "string"
                        },
                        "p_taskconfig_json": {
                            "value": {
                                "value": "@activity('nb_get_notes_taskconfig').output.result.exitValue",
                                "type": "Expression"
                            },
                            "type": "string"
                        },
                        "p_method": {
                            "value": "3pClaimNote",
                            "type": "string"
                        },
                        "p_bronze_config_id": {
                            "value": 34,
                            "type": "int"
                        },
                        "p_silver_config_id": {
                            "value": 35,
                            "type": "int"
                        }
                    }
                }
            },
            {
                "name": "Prepare_Arnote_Gold_Table",
                "type": "Script",
                "state": "Inactive",
                "onInactiveMarkAs": "Succeeded",
                "dependsOn": [
                    {
                        "activity": "nb_3parnote_bronze_to_silver",
                        "dependencyConditions": [
                            "Succeeded"
                        ]
                    }
                ],
                "policy": {
                    "timeout": "0.12:00:00",
                    "retry": 0,
                    "retryIntervalInSeconds": 30,
                    "secureOutput": false,
                    "secureInput": false
                },
                "linkedService": {
                    "name": "bhg_gold",
                    "properties": {
                        "annotations": [],
                        "type": "DataWarehouse",
                        "typeProperties": {
                            "endpoint": "ziupvjpf2lfe3ey7dnmuxchh44-72wedenf6jnuvbnc3cugwrucbq.datawarehouse.fabric.microsoft.com",
                            "artifactId": "d29ef036-8c2c-40b0-a8e0-3279f9a906e7",
                            "workspaceId": "9141acfe-f2a5-4a5b-85a2-d8a86b46820c"
                        }
                    }
                },
                "typeProperties": {
                    "scripts": [
                        {
                            "type": "Query",
                            "text": {
                                "value": "@concat('DROP TABLE IF EXISTS pats.[gd_3p_arnote_v_', replace(pipeline().RunId, '-', '_'), '];\nCREATE TABLE pats.[gd_3p_arnote_v_', replace(pipeline().RunId, '-', '_'), '] AS\nSELECT *\nFROM pats.gd_3p_arnote\nWHERE 1 = 0;')",
                                "type": "Expression"
                            }
                        }
                    ],
                    "scriptBlockExecutionTimeout": "02:00:00"
                }
            },
            {
                "name": "Prepare_ClaimNote_Gold_Table",
                "type": "Script",
                "state": "Inactive",
                "onInactiveMarkAs": "Succeeded",
                "dependsOn": [
                    {
                        "activity": "nb_3pclaimnote_bronze_to_silver",
                        "dependencyConditions": [
                            "Succeeded"
                        ]
                    }
                ],
                "policy": {
                    "timeout": "0.12:00:00",
                    "retry": 0,
                    "retryIntervalInSeconds": 30,
                    "secureOutput": false,
                    "secureInput": false
                },
                "linkedService": {
                    "name": "bhg_gold",
                    "properties": {
                        "annotations": [],
                        "type": "DataWarehouse",
                        "typeProperties": {
                            "endpoint": "ziupvjpf2lfe3ey7dnmuxchh44-72wedenf6jnuvbnc3cugwrucbq.datawarehouse.fabric.microsoft.com",
                            "artifactId": "d29ef036-8c2c-40b0-a8e0-3279f9a906e7",
                            "workspaceId": "9141acfe-f2a5-4a5b-85a2-d8a86b46820c"
                        }
                    }
                },
                "typeProperties": {
                    "scripts": [
                        {
                            "type": "Query",
                            "text": {
                                "value": "@concat('DROP TABLE IF EXISTS pats.[gd_3p_claim_note_v_', replace(pipeline().RunId, '-', '_'), '];\n\nCREATE TABLE pats.[gd_3p_claim_note_v_', replace(pipeline().RunId, '-', '_'), '] AS\nSELECT *\nFROM pats.gd_3p_claim_note\nWHERE 1 = 0;')",
                                "type": "Expression"
                            }
                        }
                    ],
                    "scriptBlockExecutionTimeout": "02:00:00"
                }
            },
            {
                "name": "copy_3parnote_silver_to_gold",
                "type": "Copy",
                "state": "Inactive",
                "onInactiveMarkAs": "Succeeded",
                "dependsOn": [
                    {
                        "activity": "Prepare_Arnote_Gold_Table",
                        "dependencyConditions": [
                            "Succeeded"
                        ]
                    }
                ],
                "policy": {
                    "timeout": "0.12:00:00",
                    "retry": 0,
                    "retryIntervalInSeconds": 30,
                    "secureOutput": false,
                    "secureInput": false
                },
                "typeProperties": {
                    "source": {
                        "type": "LakehouseTableSource",
                        "datasetSettings": {
                            "annotations": [],
                            "linkedService": {
                                "name": "bhg_silver",
                                "properties": {
                                    "annotations": [],
                                    "type": "Lakehouse",
                                    "typeProperties": {
                                        "workspaceId": "c5097ffb-b78e-441d-9575-a82bac23cac8",
                                        "artifactId": "dd09d8b6-d862-4954-a0b2-fcf7372c6595",
                                        "rootFolder": "Tables"
                                    }
                                }
                            },
                            "type": "LakehouseTable",
                            "schema": [],
                            "typeProperties": {
                                "schema": "pats",
                                "table": "sl_tbl_3parnote"
                            }
                        }
                    },
                    "sink": {
                        "type": "DataWarehouseSink",
                        "allowCopyCommand": true,
                        "writeBehavior": "Insert",
                        "datasetSettings": {
                            "annotations": [],
                            "linkedService": {
                                "name": "bhg_gold",
                                "properties": {
                                    "annotations": [],
                                    "type": "DataWarehouse",
                                    "typeProperties": {
                                        "endpoint": "ziupvjpf2lfe3ey7dnmuxchh44-72wedenf6jnuvbnc3cugwrucbq.datawarehouse.fabric.microsoft.com",
                                        "artifactId": "d29ef036-8c2c-40b0-a8e0-3279f9a906e7",
                                        "workspaceId": "9141acfe-f2a5-4a5b-85a2-d8a86b46820c"
                                    }
                                }
                            },
                            "type": "DataWarehouseTable",
                            "schema": [],
                            "typeProperties": {
                                "schema": "pats",
                                "table": {
                                    "value": "@concat('gd_3p_arnote_v_', replace(pipeline().RunId, '-', '_'))",
                                    "type": "Expression"
                                }
                            }
                        }
                    },
                    "enableStaging": true,
                    "translator": {
                        "type": "TabularTranslator",
                        "mappings": [
                            {
                                "source": {
                                    "name": "_site_code"
                                },
                                "sink": {
                                    "name": "SiteCode"
                                }
                            },
                            {
                                "source": {
                                    "name": "arnID"
                                },
                                "sink": {
                                    "name": "arnID"
                                }
                            },
                            {
                                "source": {
                                    "name": "arnLIID"
                                },
                                "sink": {
                                    "name": "arnLIID"
                                }
                            },
                            {
                                "source": {
                                    "name": "arnNOTE"
                                },
                                "sink": {
                                    "name": "arnNOTE"
                                }
                            },
                            {
                                "source": {
                                    "name": "arnUSER"
                                },
                                "sink": {
                                    "name": "arnUSER"
                                }
                            },
                            {
                                "source": {
                                    "name": "arnDATE"
                                },
                                "sink": {
                                    "name": "arnDATE"
                                }
                            },
                            {
                                "source": {
                                    "name": "arnDtRemoved"
                                },
                                "sink": {
                                    "name": "arnDtRemoved"
                                }
                            },
                            {
                                "source": {
                                    "name": "arnStrRemovedReason"
                                },
                                "sink": {
                                    "name": "arnStrRemovedReason"
                                }
                            },
                            {
                                "source": {
                                    "name": "arnStrRemovedUser"
                                },
                                "sink": {
                                    "name": "arnStrRemovedUser"
                                }
                            },
                            {
                                "source": {
                                    "name": "bid"
                                },
                                "sink": {
                                    "name": "bid"
                                }
                            },
                            {
                                "source": {
                                    "name": "arnDBnotes"
                                },
                                "sink": {
                                    "name": "arnDBnotes"
                                }
                            },
                            {
                                "source": {
                                    "name": "globalBatchId"
                                },
                                "sink": {
                                    "name": "globalBatchId"
                                }
                            },
                            {
                                "source": {
                                    "name": "RowChkSum"
                                },
                                "sink": {
                                    "name": "RowChkSum"
                                }
                            },
                            {
                                "source": {
                                    "name": "LastModAt"
                                },
                                "sink": {
                                    "name": "LastModAt"
                                }
                            },
                            {
                                "source": {
                                    "name": "RowState"
                                },
                                "sink": {
                                    "name": "RowState"
                                }
                            }
                        ],
                        "typeConversion": true,
                        "typeConversionSettings": {
                            "allowDataTruncation": true,
                            "treatBooleanAsNumber": false
                        }
                    }
                }
            },
            {
                "name": "copy_3pclaimnote_silver_to_gold",
                "type": "Copy",
                "state": "Inactive",
                "onInactiveMarkAs": "Succeeded",
                "dependsOn": [
                    {
                        "activity": "Prepare_ClaimNote_Gold_Table",
                        "dependencyConditions": [
                            "Succeeded"
                        ]
                    }
                ],
                "policy": {
                    "timeout": "0.12:00:00",
                    "retry": 0,
                    "retryIntervalInSeconds": 30,
                    "secureOutput": false,
                    "secureInput": false
                },
                "typeProperties": {
                    "source": {
                        "type": "LakehouseTableSource",
                        "datasetSettings": {
                            "annotations": [],
                            "linkedService": {
                                "name": "bhg_silver",
                                "properties": {
                                    "annotations": [],
                                    "type": "Lakehouse",
                                    "typeProperties": {
                                        "workspaceId": "c5097ffb-b78e-441d-9575-a82bac23cac8",
                                        "artifactId": "dd09d8b6-d862-4954-a0b2-fcf7372c6595",
                                        "rootFolder": "Tables"
                                    }
                                }
                            },
                            "type": "LakehouseTable",
                            "schema": [],
                            "typeProperties": {
                                "schema": "pats",
                                "table": "sl_tbl_3pclaimnote"
                            }
                        }
                    },
                    "sink": {
                        "type": "DataWarehouseSink",
                        "allowCopyCommand": true,
                        "writeBehavior": "Insert",
                        "datasetSettings": {
                            "annotations": [],
                            "linkedService": {
                                "name": "bhg_gold",
                                "properties": {
                                    "annotations": [],
                                    "type": "DataWarehouse",
                                    "typeProperties": {
                                        "endpoint": "ziupvjpf2lfe3ey7dnmuxchh44-72wedenf6jnuvbnc3cugwrucbq.datawarehouse.fabric.microsoft.com",
                                        "artifactId": "d29ef036-8c2c-40b0-a8e0-3279f9a906e7",
                                        "workspaceId": "9141acfe-f2a5-4a5b-85a2-d8a86b46820c"
                                    }
                                }
                            },
                            "type": "DataWarehouseTable",
                            "schema": [],
                            "typeProperties": {
                                "schema": "pats",
                                "table": {
                                    "value": "@concat('gd_3p_claim_note_v_', replace(pipeline().RunId, '-', '_'))",
                                    "type": "Expression"
                                }
                            }
                        }
                    },
                    "enableStaging": true,
                    "translator": {
                        "type": "TabularTranslator",
                        "mappings": [
                            {
                                "source": {
                                    "name": "_site_code"
                                },
                                "sink": {
                                    "name": "SiteCode"
                                }
                            },
                            {
                                "source": {
                                    "name": "tpcn"
                                },
                                "sink": {
                                    "name": "tpcn"
                                }
                            },
                            {
                                "source": {
                                    "name": "tpcnTPCID"
                                },
                                "sink": {
                                    "name": "tpcnTPCID"
                                }
                            },
                            {
                                "source": {
                                    "name": "tpcnDtmAdded"
                                },
                                "sink": {
                                    "name": "tpcnDtmAdded"
                                }
                            },
                            {
                                "source": {
                                    "name": "tpcnStrAdded"
                                },
                                "sink": {
                                    "name": "tpcnStrAdded"
                                }
                            },
                            {
                                "source": {
                                    "name": "tpcnStrNote"
                                },
                                "sink": {
                                    "name": "tpcnStrNote"
                                }
                            },
                            {
                                "source": {
                                    "name": "tpcnStrType"
                                },
                                "sink": {
                                    "name": "tpcnStrType"
                                }
                            },
                            {
                                "source": {
                                    "name": "tpcnDtTickler"
                                },
                                "sink": {
                                    "name": "tpcnDtTickler"
                                }
                            },
                            {
                                "source": {
                                    "name": "tpcnDtTicklerRemoved"
                                },
                                "sink": {
                                    "name": "tpcnDtTicklerRemoved"
                                }
                            },
                            {
                                "source": {
                                    "name": "tpcnStrTicklerRemovedNote"
                                },
                                "sink": {
                                    "name": "tpcnStrTicklerRemovedNote"
                                }
                            },
                            {
                                "source": {
                                    "name": "tpcnStrTicklerRemovedUser"
                                },
                                "sink": {
                                    "name": "tpcnStrTicklerRemovedUser"
                                }
                            },
                            {
                                "source": {
                                    "name": "tpcnStrTicklerType"
                                },
                                "sink": {
                                    "name": "tpcnStrTicklerType"
                                }
                            },
                            {
                                "source": {
                                    "name": "globalBatchId"
                                },
                                "sink": {
                                    "name": "globalBatchId"
                                }
                            },
                            {
                                "source": {
                                    "name": "RowChkSum"
                                },
                                "sink": {
                                    "name": "RowChkSum"
                                }
                            },
                            {
                                "source": {
                                    "name": "LastModAt"
                                },
                                "sink": {
                                    "name": "LastModAt"
                                }
                            },
                            {
                                "source": {
                                    "name": "RowState"
                                },
                                "sink": {
                                    "name": "RowState"
                                }
                            }
                        ],
                        "typeConversion": true,
                        "typeConversionSettings": {
                            "allowDataTruncation": true,
                            "treatBooleanAsNumber": false
                        }
                    }
                }
            },
            {
                "name": "Publish_Arnote_Gold",
                "type": "Script",
                "state": "Inactive",
                "onInactiveMarkAs": "Succeeded",
                "dependsOn": [
                    {
                        "activity": "copy_3parnote_silver_to_gold",
                        "dependencyConditions": [
                            "Succeeded"
                        ]
                    }
                ],
                "policy": {
                    "timeout": "0.12:00:00",
                    "retry": 0,
                    "retryIntervalInSeconds": 30,
                    "secureOutput": false,
                    "secureInput": false
                },
                "linkedService": {
                    "name": "bhg_gold",
                    "properties": {
                        "annotations": [],
                        "type": "DataWarehouse",
                        "typeProperties": {
                            "endpoint": "ziupvjpf2lfe3ey7dnmuxchh44-72wedenf6jnuvbnc3cugwrucbq.datawarehouse.fabric.microsoft.com",
                            "artifactId": "d29ef036-8c2c-40b0-a8e0-3279f9a906e7",
                            "workspaceId": "9141acfe-f2a5-4a5b-85a2-d8a86b46820c"
                        }
                    }
                },
                "typeProperties": {
                    "scripts": [
                        {
                            "type": "Query",
                            "text": {
                                "value": "@concat('DECLARE @arn_count BIGINT;\nDECLARE @arn_final_count BIGINT;\nSELECT @arn_count = COUNT(*)\nFROM pats.[gd_3p_arnote_v_', replace(pipeline().RunId, '-', '_'), '];\nIF @arn_count = 0\n    THROW 51001, ''Validation failed: pats.gd_3p_arnote_v_', replace(pipeline().RunId, '-', '_'), ' is empty.'', 1;\nIF OBJECT_ID(''pats.gd_3p_arnote'', ''U'') IS NULL\n    THROW 51003, ''Publish failed: production table pats.gd_3p_arnote does not exist.'', 1;\nDROP TABLE IF EXISTS pats.[gd_3p_arnoteBK_', replace(pipeline().RunId, '-', '_'), '];\nEXEC sp_rename ''pats.gd_3p_arnote'', ''gd_3p_arnoteBK_', replace(pipeline().RunId, '-', '_'), ''';\nEXEC sp_rename ''pats.[gd_3p_arnote_v_', replace(pipeline().RunId, '-', '_'), ']'', ''gd_3p_arnote'';\nSELECT @arn_final_count = COUNT(*)\nFROM pats.gd_3p_arnote;\nIF @arn_final_count = 0\n    THROW 51005, ''Final verification failed: pats.gd_3p_arnote is empty after table swap.'', 1;\nBEGIN TRY\n    DECLARE @cleanup_sql NVARCHAR(MAX);\n    SELECT @cleanup_sql = STRING_AGG(''DROP TABLE pats.'' + QUOTENAME(t.name), '';'' + CHAR(10))\n    FROM sys.tables t\n    INNER JOIN sys.schemas s ON t.schema_id = s.schema_id\n    WHERE s.name = ''pats''\n      AND ((t.name LIKE ''gd_3p_arnoteBK_%'' AND t.name <> ''gd_3p_arnoteBK_', replace(pipeline().RunId, '-', '_'), ''')\n         OR t.name LIKE ''gd_3p_arnote_v_%'');\n    IF @cleanup_sql IS NOT NULL AND LEN(@cleanup_sql) > 0\n        EXEC sp_executesql @cleanup_sql;\nEND TRY\nBEGIN CATCH\n    PRINT ''Cleanup warning: '' + ERROR_MESSAGE();\nEND CATCH;\nSELECT ''gd_3p_arnote'' AS TableName, @arn_final_count AS [RowCount];')",
                                "type": "Expression"
                            }
                        }
                    ],
                    "scriptBlockExecutionTimeout": "02:00:00"
                }
            },
            {
                "name": "Publish_ClaimNote_Gold",
                "type": "Script",
                "state": "Inactive",
                "onInactiveMarkAs": "Succeeded",
                "dependsOn": [
                    {
                        "activity": "copy_3pclaimnote_silver_to_gold",
                        "dependencyConditions": [
                            "Succeeded"
                        ]
                    }
                ],
                "policy": {
                    "timeout": "0.12:00:00",
                    "retry": 0,
                    "retryIntervalInSeconds": 30,
                    "secureOutput": false,
                    "secureInput": false
                },
                "linkedService": {
                    "name": "bhg_gold",
                    "properties": {
                        "annotations": [],
                        "type": "DataWarehouse",
                        "typeProperties": {
                            "endpoint": "ziupvjpf2lfe3ey7dnmuxchh44-72wedenf6jnuvbnc3cugwrucbq.datawarehouse.fabric.microsoft.com",
                            "artifactId": "d29ef036-8c2c-40b0-a8e0-3279f9a906e7",
                            "workspaceId": "9141acfe-f2a5-4a5b-85a2-d8a86b46820c"
                        }
                    }
                },
                "typeProperties": {
                    "scripts": [
                        {
                            "type": "Query",
                            "text": {
                                "value": "@concat('DECLARE @claim_count BIGINT;\nDECLARE @claim_final_count BIGINT;\nSELECT @claim_count = COUNT(*)\nFROM pats.[gd_3p_claim_note_v_', replace(pipeline().RunId, '-', '_'), '];\nIF @claim_count = 0\n    THROW 51002, ''Validation failed: pats.gd_3p_claim_note_v_', replace(pipeline().RunId, '-', '_'), ' is empty.'', 1;\nIF OBJECT_ID(''pats.gd_3p_claim_note'', ''U'') IS NULL\n    THROW 51004, ''Publish failed: production table pats.gd_3p_claim_note does not exist.'', 1;\nDROP TABLE IF EXISTS pats.[gd_3p_claim_noteBK_', replace(pipeline().RunId, '-', '_'), '];\nEXEC sp_rename ''pats.gd_3p_claim_note'', ''gd_3p_claim_noteBK_', replace(pipeline().RunId, '-', '_'), ''';\nEXEC sp_rename ''pats.[gd_3p_claim_note_v_', replace(pipeline().RunId, '-', '_'), ']'', ''gd_3p_claim_note'';\nSELECT @claim_final_count = COUNT(*)\nFROM pats.gd_3p_claim_note;\nIF @claim_final_count = 0\n    THROW 51006, ''Final verification failed: pats.gd_3p_claim_note is empty after table swap.'', 1;\nBEGIN TRY\n    DECLARE @cleanup_sql NVARCHAR(MAX);\n    SELECT @cleanup_sql = STRING_AGG(''DROP TABLE pats.'' + QUOTENAME(t.name), '';'' + CHAR(10))\n    FROM sys.tables t\n    INNER JOIN sys.schemas s ON t.schema_id = s.schema_id\n    WHERE s.name = ''pats''\n      AND ((t.name LIKE ''gd_3p_claim_noteBK_%'' AND t.name <> ''gd_3p_claim_noteBK_', replace(pipeline().RunId, '-', '_'), ''')\n         OR t.name LIKE ''gd_3p_claim_note_v_%'');\n    IF @cleanup_sql IS NOT NULL AND LEN(@cleanup_sql) > 0\n        EXEC sp_executesql @cleanup_sql;\nEND TRY\nBEGIN CATCH\n    PRINT ''Cleanup warning: '' + ERROR_MESSAGE();\nEND CATCH;\nSELECT ''gd_3p_claim_note'' AS TableName, @claim_final_count AS [RowCount];')",
                                "type": "Expression"
                            }
                        }
                    ],
                    "scriptBlockExecutionTimeout": "02:00:00"
                }
            },
            {
                "name": "set_notes_method_results",
                "type": "SetVariable",
                "dependsOn": [
                    {
                        "activity": "nb_3parnote_bronze_to_silver",
                        "dependencyConditions": [
                            "Succeeded",
                            "Failed",
                            "Skipped"
                        ]
                    },
                    {
                        "activity": "nb_3pclaimnote_bronze_to_silver",
                        "dependencyConditions": [
                            "Succeeded",
                            "Failed",
                            "Skipped"
                        ]
                    }
                ],
                "policy": {
                    "secureOutput": false,
                    "secureInput": false
                },
                "typeProperties": {
                    "variableName": "v_silver_method_results_json",
                    "value": {
                        "value": "@concat('{',if(equals(activity('nb_3parnote_bronze_to_silver').Status,'Succeeded'),substring(string(activity('nb_3parnote_bronze_to_silver').output.result.exitValue),1,sub(length(string(activity('nb_3parnote_bronze_to_silver').output.result.exitValue)),2)),concat('\"3pArnote\":{\"method\":\"3pArnote\",\"layer\":\"SL\",\"status\":\"',if(equals(activity('nb_3parnote_bronze_to_silver').Status,'Skipped'),'SKIPPED','FAILED'),'\",\"rows_read\":0,\"rows_inserted\":0,\"rows_updated\":0,\"rows_skipped\":0,\"message\":\"',if(equals(activity('nb_3parnote_bronze_to_silver').Status,'Skipped'),'3pArnote Silver notebook skipped','3pArnote Silver notebook failed before returning exitValue'),'\"}')),',',if(equals(activity('nb_3pclaimnote_bronze_to_silver').Status,'Succeeded'),substring(string(activity('nb_3pclaimnote_bronze_to_silver').output.result.exitValue),1,sub(length(string(activity('nb_3pclaimnote_bronze_to_silver').output.result.exitValue)),2)),concat('\"3pClaimNote\":{\"method\":\"3pClaimNote\",\"layer\":\"SL\",\"status\":\"',if(equals(activity('nb_3pclaimnote_bronze_to_silver').Status,'Skipped'),'SKIPPED','FAILED'),'\",\"rows_read\":0,\"rows_inserted\":0,\"rows_updated\":0,\"rows_skipped\":0,\"message\":\"',if(equals(activity('nb_3pclaimnote_bronze_to_silver').Status,'Skipped'),'3pClaimNote Silver notebook skipped','3pClaimNote Silver notebook failed before returning exitValue'),'\"}')),'}')",
                        "type": "Expression"
                    }
                }
            },
            {
                "name": "if_all_notes_methods_success",
                "type": "IfCondition",
                "dependsOn": [
                    {
                        "activity": "set_notes_method_results",
                        "dependencyConditions": [
                            "Succeeded"
                        ]
                    }
                ],
                "typeProperties": {
                    "expression": {
                        "value": "@and(not(contains(variables('v_bronze_method_results_json'),'FAILED')),and(not(contains(variables('v_bronze_method_results_json'),'ERROR')),and(not(contains(variables('v_bronze_method_results_json'),'SKIPPED')),and(not(contains(variables('v_silver_method_results_json'),'FAILED')),and(not(contains(variables('v_silver_method_results_json'),'ERROR')),not(contains(variables('v_silver_method_results_json'),'SKIPPED')))))))",
                        "type": "Expression"
                    },
                    "ifFalseActivities": [
                        {
                            "name": "nb_notes_audit_finalize_failure",
                            "type": "TridentNotebook",
                            "dependsOn": [],
                            "policy": {
                                "timeout": "0.12:00:00",
                                "retry": 0,
                                "retryIntervalInSeconds": 30,
                                "secureOutput": false,
                                "secureInput": false
                            },
                            "typeProperties": {
                                "notebookId": "9d0b3480-fa72-4814-ad31-7bbec83a3301",
                                "workspaceId": "c5097ffb-b78e-441d-9575-a82bac23cac8",
                                "parameters": {
                                    "p_mode": {
                                        "value": "FINALIZE_FAILURE",
                                        "type": "string"
                                    },
                                    "p_audit_context_json": {
                                        "value": {
                                            "value": "@activity('nb_notes_audit_start').output.result.exitValue",
                                            "type": "Expression"
                                        },
                                        "type": "string"
                                    },
                                    "p_ingest_run_id": {
                                        "value": {
                                            "value": "@pipeline().RunId",
                                            "type": "Expression"
                                        },
                                        "type": "string"
                                    },
                                    "p_sites_json": {
                                        "value": {
                                            "value": "@string(activity('flt_active_notes_sites').output.value)",
                                            "type": "Expression"
                                        },
                                        "type": "string"
                                    },
                                    "p_bronze_method_results_json": {
                                        "value": {
                                            "value": "@variables('v_bronze_method_results_json')",
                                            "type": "Expression"
                                        },
                                        "type": "string"
                                    },
                                    "p_silver_method_results_json": {
                                        "value": {
                                            "value": "@variables('v_silver_method_results_json')",
                                            "type": "Expression"
                                        },
                                        "type": "string"
                                    },
                                    "p_status": {
                                        "value": "FAILED",
                                        "type": "string"
                                    }
                                }
                            }
                        }
                    ],
                    "ifTrueActivities": [
                        {
                            "name": "nb_notes_audit_finalize_success",
                            "type": "TridentNotebook",
                            "dependsOn": [],
                            "policy": {
                                "timeout": "0.12:00:00",
                                "retry": 0,
                                "retryIntervalInSeconds": 30,
                                "secureOutput": false,
                                "secureInput": false
                            },
                            "typeProperties": {
                                "notebookId": "9d0b3480-fa72-4814-ad31-7bbec83a3301",
                                "workspaceId": "c5097ffb-b78e-441d-9575-a82bac23cac8",
                                "parameters": {
                                    "p_mode": {
                                        "value": "FINALIZE_SUCCESS",
                                        "type": "string"
                                    },
                                    "p_audit_context_json": {
                                        "value": {
                                            "value": "@activity('nb_notes_audit_start').output.result.exitValue",
                                            "type": "Expression"
                                        },
                                        "type": "string"
                                    },
                                    "p_ingest_run_id": {
                                        "value": {
                                            "value": "@pipeline().RunId",
                                            "type": "Expression"
                                        },
                                        "type": "string"
                                    },
                                    "p_sites_json": {
                                        "value": {
                                            "value": "@string(activity('flt_active_notes_sites').output.value)",
                                            "type": "Expression"
                                        },
                                        "type": "string"
                                    },
                                    "p_bronze_method_results_json": {
                                        "value": {
                                            "value": "@variables('v_bronze_method_results_json')",
                                            "type": "Expression"
                                        },
                                        "type": "string"
                                    },
                                    "p_silver_method_results_json": {
                                        "value": {
                                            "value": "@variables('v_silver_method_results_json')",
                                            "type": "Expression"
                                        },
                                        "type": "string"
                                    },
                                    "p_status": {
                                        "value": "SUCCESS",
                                        "type": "string"
                                    }
                                }
                            }
                        }
                    ]
                }
            },
            {
                "name": "nb_notes_notify_success",
                "type": "TridentNotebook",
                "state": "Inactive",
                "onInactiveMarkAs": "Succeeded",
                "dependsOn": [
                    {
                        "activity": "if_all_notes_methods_success",
                        "dependencyConditions": [
                            "Succeeded"
                        ]
                    }
                ],
                "policy": {
                    "timeout": "0.12:00:00",
                    "retry": 0,
                    "retryIntervalInSeconds": 30,
                    "secureOutput": false,
                    "secureInput": false
                },
                "typeProperties": {
                    "notebookId": "77c87686-120d-486b-9146-6a794d794e38",
                    "workspaceId": "c5097ffb-b78e-441d-9575-a82bac23cac8",
                    "parameters": {
                        "Pipeline_Name": {
                            "value": "Notes ETL",
                            "type": "string"
                        },
                        "Status": {
                            "value": "Succeeded",
                            "type": "string"
                        },
                        "Config_Name": {
                            "value": "SAMMS Notes",
                            "type": "string"
                        },
                        "Source_System": {
                            "value": "Notes",
                            "type": "string"
                        },
                        "Target_Name": {
                            "value": "SL",
                            "type": "string"
                        },
                        "Environment_Name": {
                            "value": "BHG-DATA-PLATFORM-CORE-DEV",
                            "type": "string"
                        },
                        "Run_Id": {
                            "value": {
                                "value": "@pipeline().RunId",
                                "type": "Expression"
                            },
                            "type": "string"
                        },
                        "Description": {
                            "value": "Notes pipeline completed successfully through Silver",
                            "type": "string"
                        },
                        "Error_Msg": {
                            "type": "string"
                        },
                        "Pipeline_StartTime": {
                            "value": {
                                "value": "@formatDateTime(convertFromUtc(pipeline().TriggerTime, 'Central Standard Time'), 'yyyy-MM-dd HH:mm:ss')",
                                "type": "Expression"
                            },
                            "type": "string"
                        },
                        "Pipeline_EndTime": {
                            "value": {
                                "value": "@formatDateTime(convertFromUtc(utcNow(), 'Central Standard Time'), 'yyyy-MM-dd HH:mm:ss')",
                                "type": "Expression"
                            },
                            "type": "string"
                        }
                    }
                }
            },
            {
                "name": "nb_notes_notify_failed",
                "type": "TridentNotebook",
                "state": "Inactive",
                "onInactiveMarkAs": "Succeeded",
                "dependsOn": [
                    {
                        "activity": "if_all_notes_methods_success",
                        "dependencyConditions": [
                            "Failed",
                            "Skipped"
                        ]
                    }
                ],
                "policy": {
                    "timeout": "0.12:00:00",
                    "retry": 0,
                    "retryIntervalInSeconds": 30,
                    "secureOutput": false,
                    "secureInput": false
                },
                "typeProperties": {
                    "notebookId": "77c87686-120d-486b-9146-6a794d794e38",
                    "workspaceId": "c5097ffb-b78e-441d-9575-a82bac23cac8",
                    "parameters": {
                        "Pipeline_Name": {
                            "value": "Notes ETL",
                            "type": "string"
                        },
                        "Status": {
                            "value": "Failed",
                            "type": "string"
                        },
                        "Config_Name": {
                            "value": "SAMMS Notes",
                            "type": "string"
                        },
                        "Source_System": {
                            "value": "Notes",
                            "type": "string"
                        },
                        "Target_Name": {
                            "value": "ALL",
                            "type": "string"
                        },
                        "Environment_Name": {
                            "value": "BHG-DATA-PLATFORM-CORE-DEV",
                            "type": "string"
                        },
                        "Run_Id": {
                            "value": {
                                "value": "@pipeline().RunId",
                                "type": "Expression"
                            },
                            "type": "string"
                        },
                        "Description": {
                            "value": "Notes pipeline failed or audit finalization failed. See error details.",
                            "type": "string"
                        },
                        "Error_Msg": {
                            "value": {
                                "value": "@if(equals(activity('if_all_notes_methods_success').Status, 'Skipped'), 'Notes final status check was skipped before notification.', if(greater(length(string(activity('if_all_notes_methods_success').error)), 2000), substring(string(activity('if_all_notes_methods_success').error), 0, 2000), string(activity('if_all_notes_methods_success').error)))",
                                "type": "Expression"
                            },
                            "type": "string"
                        },
                        "Pipeline_StartTime": {
                            "value": {
                                "value": "@formatDateTime(convertFromUtc(pipeline().TriggerTime, 'Central Standard Time'), 'yyyy-MM-dd HH:mm:ss')",
                                "type": "Expression"
                            },
                            "type": "string"
                        },
                        "Pipeline_EndTime": {
                            "value": {
                                "value": "@formatDateTime(convertFromUtc(utcNow(), 'Central Standard Time'), 'yyyy-MM-dd HH:mm:ss')",
                                "type": "Expression"
                            },
                            "type": "string"
                        }
                    }
                }
            },
            {
                "name": "nb_get_notes_taskconfig",
                "type": "TridentNotebook",
                "dependsOn": [],
                "policy": {
                    "timeout": "0.12:00:00",
                    "retry": 0,
                    "retryIntervalInSeconds": 30,
                    "secureOutput": false,
                    "secureInput": false
                },
                "typeProperties": {
                    "notebookId": "6e7b4814-5818-4715-9275-f6ad72743221",
                    "workspaceId": "c5097ffb-b78e-441d-9575-a82bac23cac8",
                    "parameters": {
                        "p_config_ids_json": {
                            "value": "[34,35]",
                            "type": "string"
                        },
                        "p_methods_json": {
                            "value": "[\"3pArnote\",\"3pClaimNote\"]",
                            "type": "string"
                        },
                        "p_only_active": {
                            "value": "true",
                            "type": "string"
                        },
                        "p_require_site": {
                            "value": "false",
                            "type": "string"
                        },
                        "p_require_database": {
                            "value": "false",
                            "type": "string"
                        },
                        "p_require_source_table": {
                            "value": "false",
                            "type": "string"
                        }
                    }
                }
            },
            {
                "name": "Executed_AfterBronz",
                "type": "InvokePipeline",
                "dependsOn": [
                    {
                        "activity": "nb_notes_audit_start",
                        "dependencyConditions": [
                            "Succeeded"
                        ]
                    }
                ],
                "policy": {
                    "timeout": "0.12:00:00",
                    "retry": 0,
                    "retryIntervalInSeconds": 30,
                    "secureOutput": false,
                    "secureInput": false
                },
                "typeProperties": {
                    "waitOnCompletion": true,
                    "operationType": "InvokeFabricPipeline",
                    "pipelineId": "61f2955b-68c9-4a3b-8da7-186a0ce8e23e",
                    "workspaceId": "c5097ffb-b78e-441d-9575-a82bac23cac8",
                    "parameters": {
                        "p_ingest_run_id": {
                            "value": "@pipeline().RunId",
                            "type": "Expression"
                        },
                        "p_work_date": {
                            "value": "@convertFromUtc(utcNow(), 'Central Standard Time', 'yyyy-MM-dd')",
                            "type": "Expression"
                        },
                        "p_lookback_days": {
                            "value": "@pipeline().parameters.p_lookback_days",
                            "type": "Expression"
                        },
                        "p_sites": {
                            "value": "@activity('flt_active_notes_sites').output.value",
                            "type": "Expression"
                        }
                    }
                },
                "externalReferences": {
                    "connection": "184a76ff-0d6d-4b32-aead-cb57bc45a349"
                }
            }
        ],
        "parameters": {
            "p_ingest_run_id": {
                "type": "string",
                "defaultValue": "test-run-001"
            },
            "p_work_date": {
                "type": "string",
                "defaultValue": "2026-07-14"
            },
            "p_lookback_days": {
                "type": "int",
                "defaultValue": 15
            }
        },
        "variables": {
            "v_bronze_method_results_json": {
                "type": "String",
                "defaultValue": "{}"
            },
            "v_silver_method_results_json": {
                "type": "String",
                "defaultValue": "{}"
            }
        },
        "lastModifiedByObjectId": "41032ad8-8248-4dd3-9ac8-0281d6ef4ebd",
        "lastPublishTime": "2026-07-16T13:45:15Z"
    }
}


// #endregion -->Parent ETL Execute_Notes



Notebook nb_3parnote_bronze_to_silver

Cell1:


from pyspark.sql.functions import col, current_timestamp, lit, row_number
from pyspark.sql.window import Window
import json

def notebook_exit(payload):
    text = json.dumps(payload, default=str, separators=(",", ":"))
    try:
        mssparkutils.notebook.exit(text)
    except NameError:
        print(text)
        raise SystemExit(text)

def result_payload(method_name, status, rows_read=0, rows_inserted=0, rows_updated=0, rows_skipped=0, message=None, site_results=None):
    body = {
        "method": method_name,
        "layer": "SL",
        "status": status,
        "rows_read": int(rows_read or 0),
        "rows_inserted": int(rows_inserted or 0),
        "rows_updated": int(rows_updated or 0),
        "rows_skipped": int(rows_skipped or 0)
    }
    if message:
        body["message"] = str(message)[:4000]
    if site_results is not None:
        body["site_results"] = site_results
    return {method_name: body}

# Pipeline passes p_ingest_run_id as a parameter.
# The try/except lets you run this manually during development.
try:
    p_ingest_run_id
except NameError:
    p_ingest_run_id = "test-run-001"

try:
    p_bronze_succeeded
except NameError:
    p_bronze_succeeded = "true"

try:
    p_sites_json
except NameError:
    p_sites_json = "[]"

try:
    p_bronze_method_results_json
except NameError:
    p_bronze_method_results_json = "{}"

bronze_had_method_failure = str(p_bronze_succeeded).lower() != "true"

def parse_json_list(raw):
    if not raw:
        return []
    try:
        parsed = json.loads(str(raw))
        return parsed if isinstance(parsed, list) else []
    except Exception:
        return []

def active_site_codes_for_method(method_name):
    sites = []
    seen = set()
    for row in parse_json_list(p_sites_json):
        if str(row.get("Method", "")).lower() == method_name.lower():
            site_code = row.get("SiteCode")
            if site_code and site_code not in seen:
                sites.append(str(site_code))
                seen.add(str(site_code))
    return sites

def build_site_results(method_name, successful_sites, failed_message=None):
    active_sites = active_site_codes_for_method(method_name)
    successful = {str(site) for site in successful_sites if site}
    results = []
    for site in active_sites:
        if site in successful:
            results.append({"site_code": site, "status": "SUCCESS"})
        elif bronze_had_method_failure:
            results.append({
                "site_code": site,
                "status": "FAILED",
                "failed_stage": "BR",
                "error_message": failed_message or f"{method_name} Bronze copy failed or did not write the site success marker."
            })
        else:
            results.append({"site_code": site, "status": "SUCCESS"})
    return results

# try:
#     p_bronze_succeeded
# except NameError:
#     p_bronze_succeeded = "true"

# if str(p_bronze_succeeded).lower() not in ("true", "1", "yes"):
#     raise Exception(f"Bronze failed for 3pArnote; skipping Silver MERGE for ingest_run_id={p_ingest_run_id}")

try:
    p_taskconfig_json
except NameError:
    p_taskconfig_json = "[]"

try:
    p_method
except NameError:
    p_method = "3pArnote"

try:
    p_silver_config_id
except NameError:
    p_silver_config_id = 35

try:
    p_bronze_config_id
except NameError:
    p_bronze_config_id = 34

def taskconfig_rows(raw):
    if raw is None:
        return []
    text = str(raw).strip()
    if text in ("", "[]", "{}"):
        return []
    parsed = json.loads(text)
    if isinstance(parsed, str):
        parsed = json.loads(parsed)
    if isinstance(parsed, list):
        return parsed
    if isinstance(parsed, dict):
        for key in ("value", "rows", "items", "taskconfig", "tasks"):
            value = parsed.get(key)
            if isinstance(value, list):
                return value
    return []

def row_value(row, name):
    if not isinstance(row, dict):
        return None
    for key in (name, name[:1].lower() + name[1:], name.lower()):
        if key in row:
            return row.get(key)
    return None

def target_from_request_body(row):
    request_body = row_value(row, "RequestBody")
    if not request_body:
        return None, None
    try:
        parsed = json.loads(str(request_body))
        full_table = parsed.get("full_table")
        if full_table:
            parts = str(full_table).split(".")
            if len(parts) >= 3:
                return parts[-2], parts[-1]
    except Exception:
        pass
    return None, None

def resolve_taskconfig_target(config_id_to_match, layer_name, default_schema, default_table):
    rows = taskconfig_rows(p_taskconfig_json)
    for row in rows:
        config_id = row_value(row, "ConfigId")
        method = row_value(row, "Method")
        if str(config_id) == str(config_id_to_match) and str(method).lower() == str(p_method).lower():
            target_schema = row_value(row, "TargetSchema")
            target_table = row_value(row, "TargetTable")
            if not target_schema or not target_table:
                body_schema, body_table = target_from_request_body(row)
                target_schema = target_schema or body_schema
                target_table = target_table or body_table
            if not target_schema or not target_table:
                raise Exception(f"{layer_name} taskconfig row for ConfigId={config_id_to_match}, Method={p_method} is missing TargetSchema/TargetTable and RequestBody.full_table.")
            return str(target_schema), str(target_table)

    if rows:
        raise Exception(f"No {layer_name} taskconfig row found for ConfigId={config_id_to_match}, Method={p_method}.")

    return default_schema, default_table

bronze_schema, bronze_target_table = resolve_taskconfig_target(p_bronze_config_id, "Bronze", "Notes", "br_tbl3pArnote")
bronze_table = f"bhg_bronze.{bronze_schema}.{bronze_target_table}"
target_schema, target_table = resolve_taskconfig_target(p_silver_config_id, "Silver", "pats", "tbl_3pARNOTE")
silver_table = f"bhg_silver.{target_schema}.{target_table}"
legacy_silver_table = f"bhg_silver.{target_schema}.sl_{target_table}"
final_columns = [
    "SiteCode",
    "arnID",
    "arnLIID",
    "arnNOTE",
    "arnUSER",
    "arnDATE",
    "arnDtRemoved",
    "arnStrRemovedReason",
    "arnStrRemovedUser",
    "bid",
    "arnDBnotes",
    "globalBatchId",
    "RowChkSum",
    "LastModAt",
    "RowState"
]

print(f"Processing ingest_run_id: {p_ingest_run_id}")
print(f"Bronze table: {bronze_table}")
print(f"Silver table: {silver_table}")

# Read only rows from THIS pipeline run.
bronze_df = spark.table(bronze_table).where(col("_ingest_run_id") == p_ingest_run_id)

bronze_count = bronze_df.count()
print(f"Bronze rows for this run: {bronze_count}")

successful_bronze_sites = [
    row["_site_code"]
    for row in (
        bronze_df
        .where(col("_site_code").isNotNull())
        .select("_site_code")
        .distinct()
        .collect()
    )
]
site_results = build_site_results("3pArnote", successful_bronze_sites)

if bronze_count == 0:
    notebook_exit(result_payload(
        "3pArnote",
        "SKIPPED" if bronze_had_method_failure else "SUCCESS",
        rows_read=0,
        message=f"No successful Bronze sites found for 3pArnote ingest_run_id = {p_ingest_run_id}",
        site_results=site_results
    ))

# Deduplicate within current run.
# Business key = _site_code + arnID   (mirrors Azure PK: SiteCode, ArnId)
# If the same record appears twice (e.g. due to a retry), keep the latest extraction.
w = Window.partitionBy("_site_code", "arnID").orderBy(col("_extracted_at").desc())

src_work_df = (
    bronze_df
    .where(col("_site_code").isNotNull() & col("arnID").isNotNull())
    .withColumn("rn", row_number().over(w))
    .where(col("rn") == 1)
    .drop("rn")

    # RowState logic: ARNote treats every returned SAMMS row as active.
    # There is no pre-reset or soft-delete condition for ARNote.
    .withColumn("RowState", lit(True))
    .withColumn("LastModAt", current_timestamp())
)

src_df = src_work_df.select(
    col("_site_code").alias("SiteCode"),
    col("arnID"),
    col("arnLIID"),
    col("arnNOTE"),
    col("arnUSER"),
    col("arnDATE"),
    col("arnDtRemoved"),
    col("arnStrRemovedReason"),
    col("arnStrRemovedUser"),
    col("bid"),
    col("arnDBnotes"),
    col("globalBatchId"),
    col("RowChkSum"),
    col("LastModAt"),
    col("RowState")
)

src_df.createOrReplaceTempView("vw_arnote_current_run")

src_count = src_df.count()
print(f"Prepared source rows for ARNote Silver: {src_count}")

# First-ever run: create Silver table
created_silver_table = False
if not spark.catalog.tableExists(silver_table):
    if spark.catalog.tableExists(legacy_silver_table):
        legacy_df = spark.table(legacy_silver_table)
        projected_cols = []
        for c in final_columns:
            if c in legacy_df.columns:
                projected_cols.append(col(c).alias(c))
            elif c == "SiteCode" and "_site_code" in legacy_df.columns:
                projected_cols.append(col("_site_code").alias("SiteCode"))
            elif c == "LastModAt" and "silver_updated_at" in legacy_df.columns:
                projected_cols.append(col("silver_updated_at").alias("LastModAt"))
            else:
                projected_cols.append(lit(None).alias(c))

        migrated_df = legacy_df.select(*projected_cols).cache()
        migrated_count = migrated_df.count()
        (
            migrated_df
            .write
            .format("delta")
            .mode("overwrite")
            .option("overwriteSchema", "true")
            .saveAsTable(silver_table)
        )
        migrated_df.unpersist()
        print(f"Migrated ARNote Silver table from {legacy_silver_table} to {silver_table}. Rows preserved: {migrated_count}")
    else:
        (
            src_df
            .write
            .format("delta")
            .mode("overwrite")
            .saveAsTable(silver_table)
        )
        created_silver_table = True
        print(f"Created ARNote Silver table and inserted rows: {src_count}")
else:
    print(f"Silver table already exists: {silver_table}")


cell2:

from delta.tables import DeltaTable

try:
    silver_table
except NameError:
    target_schema, target_table = resolve_taskconfig_target(p_silver_config_id, "Silver", "pats", "tbl_3pARNOTE")
    silver_table = f"bhg_silver.{target_schema}.{target_table}"

if not spark.catalog.tableExists(silver_table):
    raise Exception(f"Silver table does not exist: {silver_table}")

# One-time normalization for Silver tables created before Silver became the final layer.
# This removes internal Fabric columns and keeps only the former Gold/reporting columns.
existing_cols = spark.table(silver_table).columns
if existing_cols != final_columns:
    existing_df = spark.table(silver_table)
    projected_cols = []
    for c in final_columns:
        if c in existing_df.columns:
            projected_cols.append(col(c).alias(c))
        elif c == "SiteCode" and "_site_code" in existing_df.columns:
            projected_cols.append(col("_site_code").alias("SiteCode"))
        elif c == "LastModAt" and "silver_updated_at" in existing_df.columns:
            projected_cols.append(col("silver_updated_at").alias("LastModAt"))
        else:
            projected_cols.append(lit(None).alias(c))

    normalized_df = existing_df.select(*projected_cols).cache()
    normalized_count = normalized_df.count()
    (
        normalized_df
        .write
        .format("delta")
        .mode("overwrite")
        .option("overwriteSchema", "true")
        .saveAsTable(silver_table)
    )
    normalized_df.unpersist()
    print(f"Normalized ARNote Silver table to final schema. Rows preserved: {normalized_count}")

silver_delta = DeltaTable.forName(spark, silver_table)

src_cols = final_columns
update_set = {c: f"src.{c}" for c in src_cols}
insert_values = {c: f"src.{c}" for c in src_cols}

match_keys = ["SiteCode", "arnID"]
if created_silver_table:
    rows_inserted = src_count
    rows_updated = 0
else:
    target_keys = spark.table(silver_table).select(*match_keys).dropDuplicates()
    rows_inserted = src_df.join(target_keys, match_keys, "left_anti").count()
    rows_updated = (
        src_df.alias("src")
        .join(spark.table(silver_table).alias("tgt"), match_keys, "inner")
        .where("""
            tgt.RowChkSum IS NULL
            OR src.RowChkSum IS NULL
            OR tgt.RowChkSum <> src.RowChkSum
        """)
        .count()
    )

(
    silver_delta.alias("tgt")
    .merge(
        src_df.alias("src"),
        # Merge key: clinic + note ID (mirrors Azure PK: SiteCode + ArnId)
        "tgt.SiteCode = src.SiteCode AND tgt.arnID = src.arnID"
    )

    # CASE 1: Record exists AND data changed: full update
    # Full update of all data columns
    .whenMatchedUpdate(
        condition="""
            tgt.RowChkSum IS NULL
            OR src.RowChkSum IS NULL
            OR tgt.RowChkSum <> src.RowChkSum
        """,
        set=update_set
    )

    # CASE 2: New record: insert all columns
    .whenNotMatchedInsert(values=insert_values)

    .execute()
)

print("ARNote Silver MERGE completed successfully.")
notebook_exit(result_payload(
    "3pArnote",
    "SUCCESS",
    rows_read=src_count,
    rows_inserted=rows_inserted,
    rows_updated=rows_updated,
    site_results=site_results
))



nb_3pclaimnote_bronze_to_silver

Cell1:

from pyspark.sql.functions import col, current_timestamp, lit, row_number
from pyspark.sql.window import Window
import json

def notebook_exit(payload):
    text = json.dumps(payload, default=str, separators=(",", ":"))
    try:
        mssparkutils.notebook.exit(text)
    except NameError:
        print(text)
        raise SystemExit(text)

def result_payload(method_name, status, rows_read=0, rows_inserted=0, rows_updated=0, rows_skipped=0, message=None, site_results=None):
    body = {
        "method": method_name,
        "layer": "SL",
        "status": status,
        "rows_read": int(rows_read or 0),
        "rows_inserted": int(rows_inserted or 0),
        "rows_updated": int(rows_updated or 0),
        "rows_skipped": int(rows_skipped or 0)
    }
    if message:
        body["message"] = str(message)[:4000]
    if site_results is not None:
        body["site_results"] = site_results
    return {method_name: body}

try:
    p_ingest_run_id
except NameError:
    p_ingest_run_id = "test-run-001"

try:
    p_bronze_succeeded
except NameError:
    p_bronze_succeeded = "true"

try:
    p_sites_json
except NameError:
    p_sites_json = "[]"

try:
    p_bronze_method_results_json
except NameError:
    p_bronze_method_results_json = "{}"

bronze_had_method_failure = str(p_bronze_succeeded).lower() != "true"

def parse_json_list(raw):
    if not raw:
        return []
    try:
        parsed = json.loads(str(raw))
        return parsed if isinstance(parsed, list) else []
    except Exception:
        return []

def active_site_codes_for_method(method_name):
    sites = []
    seen = set()
    for row in parse_json_list(p_sites_json):
        if str(row.get("Method", "")).lower() == method_name.lower():
            site_code = row.get("SiteCode")
            if site_code and site_code not in seen:
                sites.append(str(site_code))
                seen.add(str(site_code))
    return sites

def build_site_results(method_name, successful_sites, failed_message=None):
    active_sites = active_site_codes_for_method(method_name)
    successful = {str(site) for site in successful_sites if site}
    results = []
    for site in active_sites:
        if site in successful:
            results.append({"site_code": site, "status": "SUCCESS"})
        elif bronze_had_method_failure:
            results.append({
                "site_code": site,
                "status": "FAILED",
                "failed_stage": "BR",
                "error_message": failed_message or f"{method_name} Bronze copy failed or did not write the site success marker."
            })
        else:
            results.append({"site_code": site, "status": "SUCCESS"})
    return results

# try:
#     p_bronze_succeeded
# except NameError:
#     p_bronze_succeeded = "true"

# if str(p_bronze_succeeded).lower() not in ("true", "1", "yes"):
#     raise Exception(f"Bronze failed for 3pClaimNote; skipping Silver MERGE for ingest_run_id={p_ingest_run_id}")

try:
    p_taskconfig_json
except NameError:
    p_taskconfig_json = "[]"

try:
    p_method
except NameError:
    p_method = "3pClaimNote"

try:
    p_silver_config_id
except NameError:
    p_silver_config_id = 35

try:
    p_bronze_config_id
except NameError:
    p_bronze_config_id = 34

def taskconfig_rows(raw):
    if raw is None:
        return []
    text = str(raw).strip()
    if text in ("", "[]", "{}"):
        return []
    parsed = json.loads(text)
    if isinstance(parsed, str):
        parsed = json.loads(parsed)
    if isinstance(parsed, list):
        return parsed
    if isinstance(parsed, dict):
        for key in ("value", "rows", "items", "taskconfig", "tasks"):
            value = parsed.get(key)
            if isinstance(value, list):
                return value
    return []

def row_value(row, name):
    if not isinstance(row, dict):
        return None
    for key in (name, name[:1].lower() + name[1:], name.lower()):
        if key in row:
            return row.get(key)
    return None

def target_from_request_body(row):
    request_body = row_value(row, "RequestBody")
    if not request_body:
        return None, None
    try:
        parsed = json.loads(str(request_body))
        full_table = parsed.get("full_table")
        if full_table:
            parts = str(full_table).split(".")
            if len(parts) >= 3:
                return parts[-2], parts[-1]
    except Exception:
        pass
    return None, None

def resolve_taskconfig_target(config_id_to_match, layer_name, default_schema, default_table):
    rows = taskconfig_rows(p_taskconfig_json)
    for row in rows:
        config_id = row_value(row, "ConfigId")
        method = row_value(row, "Method")
        if str(config_id) == str(config_id_to_match) and str(method).lower() == str(p_method).lower():
            target_schema = row_value(row, "TargetSchema")
            target_table = row_value(row, "TargetTable")
            if not target_schema or not target_table:
                body_schema, body_table = target_from_request_body(row)
                target_schema = target_schema or body_schema
                target_table = target_table or body_table
            if not target_schema or not target_table:
                raise Exception(f"{layer_name} taskconfig row for ConfigId={config_id_to_match}, Method={p_method} is missing TargetSchema/TargetTable and RequestBody.full_table.")
            return str(target_schema), str(target_table)

    if rows:
        raise Exception(f"No {layer_name} taskconfig row found for ConfigId={config_id_to_match}, Method={p_method}.")

    return default_schema, default_table

bronze_schema, bronze_target_table = resolve_taskconfig_target(p_bronze_config_id, "Bronze", "Notes", "br_tbl3pClaimNote")
bronze_table = f"bhg_bronze.{bronze_schema}.{bronze_target_table}"
target_schema, target_table = resolve_taskconfig_target(p_silver_config_id, "Silver", "pats", "tbl_3pClaimNote")
silver_table = f"bhg_silver.{target_schema}.{target_table}"
legacy_silver_table = f"bhg_silver.{target_schema}.sl_{target_table}"
final_columns = [
    "SiteCode",
    "tpcn",
    "tpcnTPCID",
    "tpcnDtmAdded",
    "tpcnStrAdded",
    "tpcnStrNote",
    "tpcnStrType",
    "tpcnDtTickler",
    "tpcnDtTicklerRemoved",
    "tpcnStrTicklerRemovedNote",
    "tpcnStrTicklerRemovedUser",
    "tpcnStrTicklerType",
    "globalBatchId",
    "RowChkSum",
    "LastModAt",
    "RowState"
]

print(f"Processing ingest_run_id: {p_ingest_run_id}")
print(f"Bronze table: {bronze_table}")
print(f"Silver table: {silver_table}")

bronze_df = spark.table(bronze_table).where(col("_ingest_run_id") == p_ingest_run_id)

bronze_count = bronze_df.count()
print(f"Bronze rows for this run: {bronze_count}")

successful_bronze_sites = [
    row["_site_code"]
    for row in (
        bronze_df
        .where(col("_site_code").isNotNull())
        .select("_site_code")
        .distinct()
        .collect()
    )
]
site_results = build_site_results("3pClaimNote", successful_bronze_sites)

if bronze_count == 0:
    notebook_exit(result_payload(
        "3pClaimNote",
        "SKIPPED" if bronze_had_method_failure else "SUCCESS",
        rows_read=0,
        message=f"No successful Bronze sites found for 3pClaimNote ingest_run_id = {p_ingest_run_id}",
        site_results=site_results
    ))

# Critical: deduplicate on _site_code + tpcnTPCID.
# The C# Save3pClaimNote matches existing records by TpcnTpcid, NOT by tpcn
# (the Azure PK). See Section 20 for full explanation.
# For Fabric, we use the same match logic: _site_code + tpcnTPCID.
w = Window.partitionBy("_site_code", "tpcnTPCID").orderBy(col("_extracted_at").desc())

src_work_df = (
    bronze_df
    .where(col("_site_code").isNotNull() & col("tpcnTPCID").isNotNull())
    .withColumn("rn", row_number().over(w))
    .where(col("rn") == 1)
    .drop("rn")

    # RowState logic: ClaimNote treats every returned SAMMS row as active.
    # There is no pre-reset or soft-delete condition for ClaimNote.
    .withColumn("RowState", lit(True))
    .withColumn("LastModAt", current_timestamp())
)

src_df = src_work_df.select(
    col("_site_code").alias("SiteCode"),
    col("tpcn"),
    col("tpcnTPCID"),
    col("tpcnDtmAdded"),
    col("tpcnStrAdded"),
    col("tpcnStrNote"),
    col("tpcnStrType"),
    col("tpcnDtTickler"),
    col("tpcnDtTicklerRemoved"),
    col("tpcnStrTicklerRemovedNote"),
    col("tpcnStrTicklerRemovedUser"),
    col("tpcnStrTicklerType"),
    col("globalBatchId"),
    col("RowChkSum"),
    col("LastModAt"),
    col("RowState")
)

src_df.createOrReplaceTempView("vw_claimnote_current_run")

src_count = src_df.count()
print(f"Prepared source rows for ClaimNote Silver: {src_count}")

created_silver_table = False
if not spark.catalog.tableExists(silver_table):
    if spark.catalog.tableExists(legacy_silver_table):
        legacy_df = spark.table(legacy_silver_table)
        projected_cols = []
        for c in final_columns:
            if c in legacy_df.columns:
                projected_cols.append(col(c).alias(c))
            elif c == "SiteCode" and "_site_code" in legacy_df.columns:
                projected_cols.append(col("_site_code").alias("SiteCode"))
            elif c == "LastModAt" and "silver_updated_at" in legacy_df.columns:
                projected_cols.append(col("silver_updated_at").alias("LastModAt"))
            else:
                projected_cols.append(lit(None).alias(c))

        migrated_df = legacy_df.select(*projected_cols).cache()
        migrated_count = migrated_df.count()
        (
            migrated_df
            .write
            .format("delta")
            .mode("overwrite")
            .option("overwriteSchema", "true")
            .saveAsTable(silver_table)
        )
        migrated_df.unpersist()
        print(f"Migrated ClaimNote Silver table from {legacy_silver_table} to {silver_table}. Rows preserved: {migrated_count}")
    else:
        (
            src_df
            .write
            .format("delta")
            .mode("overwrite")
            .saveAsTable(silver_table)
        )
        created_silver_table = True
        print(f"Created ClaimNote Silver table: {src_count}")
else:
    print(f"Silver table exists: {silver_table}")


cell2:

from delta.tables import DeltaTable

try:
    silver_table
except NameError:
    target_schema, target_table = resolve_taskconfig_target(p_silver_config_id, "Silver", "pats", "tbl_3pClaimNote")
    silver_table = f"bhg_silver.{target_schema}.{target_table}"

if not spark.catalog.tableExists(silver_table):
    raise Exception(f"Silver table does not exist: {silver_table}")

# One-time normalization for Silver tables created before Silver became the final layer.
# This removes internal Fabric columns and keeps only the former Gold/reporting columns.
existing_cols = spark.table(silver_table).columns
if existing_cols != final_columns:
    existing_df = spark.table(silver_table)
    projected_cols = []
    for c in final_columns:
        if c in existing_df.columns:
            projected_cols.append(col(c).alias(c))
        elif c == "SiteCode" and "_site_code" in existing_df.columns:
            projected_cols.append(col("_site_code").alias("SiteCode"))
        elif c == "LastModAt" and "silver_updated_at" in existing_df.columns:
            projected_cols.append(col("silver_updated_at").alias("LastModAt"))
        else:
            projected_cols.append(lit(None).alias(c))

    normalized_df = existing_df.select(*projected_cols).cache()
    normalized_count = normalized_df.count()
    (
        normalized_df
        .write
        .format("delta")
        .mode("overwrite")
        .option("overwriteSchema", "true")
        .saveAsTable(silver_table)
    )
    normalized_df.unpersist()
    print(f"Normalized ClaimNote Silver table to final schema. Rows preserved: {normalized_count}")

silver_delta = DeltaTable.forName(spark, silver_table)

src_cols = final_columns
update_set = {c: f"src.{c}" for c in src_cols}
insert_values = {c: f"src.{c}" for c in src_cols}

match_keys = ["SiteCode", "tpcnTPCID"]
if created_silver_table:
    rows_inserted = src_count
    rows_updated = 0
else:
    target_keys = spark.table(silver_table).select(*match_keys).dropDuplicates()
    rows_inserted = src_df.join(target_keys, match_keys, "left_anti").count()
    rows_updated = (
        src_df.alias("src")
        .join(spark.table(silver_table).alias("tgt"), match_keys, "inner")
        .where("""
            tgt.RowChkSum IS NULL
            OR src.RowChkSum IS NULL
            OR tgt.RowChkSum <> src.RowChkSum
        """)
        .count()
    )

(
    silver_delta.alias("tgt")
    .merge(
        src_df.alias("src"),
        # Critical: match on tpcnTPCID, not tpcn.
        # The C# Save3pClaimNote line 376:
        #   tblCNs.FirstOrDefault(x => x.TpcnTpcid == claimNote.TpcnTpcid)
        # Uses TpcnTpcid as the match key, not the Azure PK (tpcn).
        # Fabric MERGE must mirror this: SiteCode + tpcnTPCID
        "tgt.SiteCode = src.SiteCode AND tgt.tpcnTPCID = src.tpcnTPCID"
    )

    # CASE 1: Record exists AND data changed: full update
    .whenMatchedUpdate(
        condition="""
            tgt.RowChkSum IS NULL
            OR src.RowChkSum IS NULL
            OR tgt.RowChkSum <> src.RowChkSum
        """,
        set=update_set
    )

    # CASE 2: New record: insert all columns
    .whenNotMatchedInsert(values=insert_values)

    .execute()
)

print("ClaimNote Silver MERGE completed successfully.")
notebook_exit(result_payload(
    "3pClaimNote",
    "SUCCESS",
    rows_read=src_count,
    rows_inserted=rows_inserted,
    rows_updated=rows_updated,
    site_results=site_results
))


