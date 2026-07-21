

Parent:

{
    "name": "pl_dose",
    "objectId": "a3401580-ada4-49c7-8efe-55a94295a020",
    "properties": {
        "activities": [
            {
                "name": "Src_to_Brz",
                "type": "InvokePipeline",
                "dependsOn": [
                    {
                        "activity": "control_audit_dose",
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
                    "pipelineId": "b3b79e02-d56b-4f2a-b68e-289793c8d8d5",
                    "workspaceId": "c5097ffb-b78e-441d-9575-a82bac23cac8",
                    "parameters": {
                        "p_sites": {
                            "value": "@activity('fliter_Active_Sitecodes').output.value",
                            "type": "Expression"
                        },
                        "p_ingest_run_id": {
                            "value": "@pipeline().RunId",
                            "type": "Expression"
                        },
                        "p_lookback_days": {
                            "value": "@pipeline().parameters.p_lookback_days",
                            "type": "Expression"
                        },
                        "p_audit_context_json": {
                            "value": "@activity('control_audit_dose').output.result.exitvalue",
                            "type": "Expression"
                        },
                        "p_work_date": "2026-07-06"
                    }
                },
                "externalReferences": {
                    "connection": "184a76ff-0d6d-4b32-aead-cb57bc45a349"
                }
            },
            {
                "name": "fliter_Active_Sitecodes",
                "type": "Filter",
                "dependsOn": [
                    {
                        "activity": "Etlconfigs",
                        "dependencyConditions": [
                            "Succeeded"
                        ]
                    }
                ],
                "typeProperties": {
                    "items": {
                        "value": "@json(activity('Etlconfigs').output.result.exitValue)",
                        "type": "Expression"
                    },
                    "condition": {
                        "value": "@and(\n    and(\n        and(\n            equals(item().ConfigId, 7),\n            or(\n                equals(item().TaskName, 'Bronze DoseExcuse'),\n                equals(item().TaskName, 'Bronze Dose')\n            )\n        ),\n        equals(item().IsActive, 1)\n    ),\n    and(\n        not(equals(item().SiteCode, null)),\n        not(equals(item().DataBaseName, null))\n    )\n)",
                        "type": "Expression"
                    }
                }
            },
            {
                "name": "control_audit_dose",
                "type": "TridentNotebook",
                "dependsOn": [
                    {
                        "activity": "fliter_Active_Sitecodes",
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
                    "notebookId": "2ba7000b-89f6-4e40-ac7f-7787792e2ee8",
                    "workspaceId": "c5097ffb-b78e-441d-9575-a82bac23cac8",
                    "parameters": {
                        "p_mode": {
                            "value": "START_LAYER_RUNS",
                            "type": "string"
                        },
                        "p_config_name_prefix": {
                            "value": "SAMMS Dose",
                            "type": "string"
                        },
                        "p_pipeline_name": {
                            "value": "pl_dose",
                            "type": "string"
                        },
                        "p_pipeline_path": {
                            "value": "/pipelines/pl_dose",
                            "type": "string"
                        },
                        "p_tiggered_by": {
                            "value": "Fabric",
                            "type": "string"
                        }
                    }
                }
            },
            {
                "name": "nb_dose_failure_notification",
                "type": "TridentNotebook",
                "state": "Inactive",
                "onInactiveMarkAs": "Succeeded",
                "dependsOn": [
                    {
                        "activity": "If Condition1",
                        "dependencyConditions": [
                            "Skipped",
                            "Failed"
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
                    "notebookId": "7590b2e2-50bb-4694-9ef9-0d1f4c02b156",
                    "workspaceId": "c5097ffb-b78e-441d-9575-a82bac23cac8",
                    "parameters": {
                        "Pipeline_Name": {
                            "value": "Dose ETL",
                            "type": "string"
                        },
                        "Status": {
                            "value": "Failed",
                            "type": "string"
                        },
                        "Config_Name": {
                            "value": "SAMMS Dose",
                            "type": "string"
                        },
                        "Source_System": {
                            "value": "Dose",
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
                            "value": "Dose pipeline failed or audit finalization failed. See error details.",
                            "type": "string"
                        },
                        "Error_Msg": {
                            "value": {
                                "value": "@if(and(not(empty(variables('v_method_results_json'))), not(equals(variables('v_method_results_json'), '{}'))), variables('v_method_results_json'), 'Dose pipeline failed. Check the pipeline run activity grid for details.')",
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
                                "value": "@formatDateTime(convertFromUtc(pipeline().TriggerTime, 'Central Standard Time'), 'yyyy-MM-dd HH:mm:ss')",
                                "type": "Expression"
                            },
                            "type": "string"
                        }
                    }
                }
            },
            {
                "name": "Etlconfigs",
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
                    "workspaceId": "c5097ffb-b78e-441d-9575-a82bac23cac8"
                }
            },
            {
                "name": "doseExcuse_silver_to_gold",
                "type": "TridentNotebook",
                "state": "Inactive",
                "onInactiveMarkAs": "Succeeded",
                "dependsOn": [
                    {
                        "activity": "doses_excuse_bronze_to_silver",
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
                    "notebookId": "542c3437-b0dc-46b3-a002-eb2c5f7d3d62",
                    "workspaceId": "c5097ffb-b78e-441d-9575-a82bac23cac8"
                }
            },
            {
                "name": "dose_silver_to_gold",
                "type": "TridentNotebook",
                "state": "Inactive",
                "onInactiveMarkAs": "Succeeded",
                "dependsOn": [
                    {
                        "activity": "dose_bronze_to_silver",
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
                    "notebookId": "f2030b15-4768-4cea-90d3-d9a168bbb799",
                    "workspaceId": "c5097ffb-b78e-441d-9575-a82bac23cac8"
                }
            },
            {
                "name": "doses_excuse_bronze_to_silver",
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
                    "notebookId": "72d50d83-99ab-4d6b-981d-939486313012",
                    "workspaceId": "c5097ffb-b78e-441d-9575-a82bac23cac8"
                }
            },
            {
                "name": "dose_bronze_to_silver",
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
                    "notebookId": "658d5662-6be6-4ef7-b3e2-a68e52c4ecf8",
                    "workspaceId": "c5097ffb-b78e-441d-9575-a82bac23cac8"
                }
            },
            {
                "name": "set_bronze_method_results_from_child",
                "type": "SetVariable",
                "dependsOn": [
                    {
                        "activity": "Src_to_Brz",
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
                        "value": "@coalesce(if(and(contains(activity('Src_to_Brz').output,'pipelineReturnValue'),contains(activity('Src_to_Brz').output.pipelineReturnValue,'v_bronze_method_results_json')),\n        string(activity('Src_to_Brz').output.pipelineReturnValue['v_bronze_method_results_json']),null),\nif(and(contains(activity('Src_to_Brz').output,'PipelineReturnValue'),\ncontains(activity('Src_to_Brz').output.PipelineReturnValue,'v_bronze_method_results_json')),     string(activity('Src_to_Brz').output.PipelineReturnValue['v_bronze_method_results_json']),null),'{','\"DoseExcuse \":\n{\"status\":\"FAILED\",\"failed_stage\":\"BR\",\"error_message\":\"Child Bronze pipeline did not return method-level result for DoseExcuse\"},\"Dose\":{\"status\":\"FAILS\",\"failed_stage\":\"BR\",\"error_message\":\"child bronze pipeline did not return method-level result for Dose\"}}')",
                        "type": "Expression"
                    }
                }
            },
            {
                "name": "Set_dose_method_results",
                "type": "SetVariable",
                "dependsOn": [
                    {
                        "activity": "dose_silver_to_gold",
                        "dependencyConditions": [
                            "Succeeded",
                            "Skipped",
                            "Failed"
                        ]
                    },
                    {
                        "activity": "doseExcuse_silver_to_gold",
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
                    "variableName": "v_silver_method_result_json",
                    "value": {
                        "value": "@concat('{',if(equals(activity('doses_excuse_bronze_to_silver').Status,'succeeded'),substring(string(activity('doses_excuse_bronze_to_silver').output.result.exitValue),1,sub(length(string(activity('doses_excuse_bronze_to_silver').output.result.exitValue)),2)),concat('\"DoseExcuse\":{\"method\":\"DoseExcuse\",\"layer\":\"SL\",\"status\":\"',if(equals(activity('doses_excuse_bronze_to_silver').Status,'Skipped'),'SKIPPED','FAILED'),'\",\"rows_read\":0,\"rows_inserted\":0,\"rows_updated\":0,\"rows_skipped\":0,\"message\":\"',if(equals(activity('doses_excuse_bronze_to_silver').Status,'Skipped'),'DosesExcuse Silver notebook skipped','DosesExcuse Silver notebook failed before returning exitValue'),'\"}')),',',if(equals(activity('dose_bronze_to_silver').Status,'succeeded'),substring(string(activity('dose_bronze_to_silver').output.result.exitValue),1,sub(length(string(activity('dose_bronze_to_silver').output.result.exitValue)),2)),concat('\"Dose\":{\"method\":\"Dose\",\"layer\":\"SL\",\"status\":\"',if(equals(activity('dose_bronze_to_silver').Status,'Skipped'),'SKIPPED','FAILED'),'\",\"rows_read\":0,\"rows_inserted\":0,\"rows_updated\":0,\"rows_skipped\":0,\"message\":\"',if(equals(activity('dose_bronze_to_silver').Status,'Skipped'),'Doses Silver notebook skipped','Doses Silver notebook failed before returning exitValue'),'\"}')),'}')",
                        "type": "Expression"
                    }
                }
            },
            {
                "name": "If Condition1",
                "type": "IfCondition",
                "dependsOn": [
                    {
                        "activity": "Set_dose_method_results",
                        "dependencyConditions": [
                            "Succeeded"
                        ]
                    }
                ],
                "typeProperties": {
                    "expression": {
                        "value": "@and(not(contains(variables('v_bronze_method_results_json'),'FAILED')),and(not(contains(variables('v_bronze_method_results_json'),'ERROR')),and(not(contains(variables('v_bronze_method_results_json'),'SKIPPED')),and(not(contains(variables('v_silver_method_result_json'),'FAILED')),and(not(contains(variables('v_silver_method_result_json'),'ERROR')),not(contains(variables('v_silver_method_result_json'),'SKIPPED')))))))",
                        "type": "Expression"
                    },
                    "ifFalseActivities": [
                        {
                            "name": "control_audit_dose_Failure",
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
                                "notebookId": "2ba7000b-89f6-4e40-ac7f-7787792e2ee8",
                                "workspaceId": "c5097ffb-b78e-441d-9575-a82bac23cac8",
                                "parameters": {
                                    "p_mode": {
                                        "value": "FINALIZE_FAILURE",
                                        "type": "string"
                                    },
                                    "p_audit_context_json": {
                                        "value": {
                                            "value": "@activity('control_audit_dose').output.result.exitValue",
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
                                            "value": "@string(activity('fliter_Active_Sitecodes').output.value)",
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
                                    "p_status": {
                                        "value": "FAILED",
                                        "type": "string"
                                    },
                                    "p_silver_method_results_json": {
                                        "value": {
                                            "value": "@variables('v_silver_method_result_json')",
                                            "type": "Expression"
                                        },
                                        "type": "string"
                                    }
                                }
                            }
                        }
                    ],
                    "ifTrueActivities": [
                        {
                            "name": "control_audit_dose_Sucess",
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
                                "notebookId": "2ba7000b-89f6-4e40-ac7f-7787792e2ee8",
                                "workspaceId": "c5097ffb-b78e-441d-9575-a82bac23cac8",
                                "parameters": {
                                    "p_mode": {
                                        "value": "FINALIZE_SUCCESS",
                                        "type": "string"
                                    },
                                    "p_audit_context_json": {
                                        "value": {
                                            "value": "@activity('control_audit_dose').output.result.exitValue",
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
                                            "value": "@string(activity('fliter_Active_Sitecodes').output.value)",
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
                                    "p_status": {
                                        "value": "SUCCESS",
                                        "type": "string"
                                    },
                                    "p_silver_method_results_json": {
                                        "value": {
                                            "value": "@variables('v_silver_method_result_json')",
                                            "type": "Expression"
                                        },
                                        "type": "string"
                                    }
                                }
                            }
                        }
                    ]
                }
            }
        ],
        "parameters": {
            "p_lookback_days": {
                "type": "int",
                "defaultValue": 15
            },
            "p_ingest_run_id": {
                "type": "string",
                "defaultValue": "manual-run"
            },
            "p_sites": {
                "type": "array",
                "defaultValue": []
            }
        },
        "variables": {
            "v_bronze_method_results_json": {
                "type": "String"
            },
            "v_method_results_json": {
                "type": "String"
            },
            "v_silver_method_result_json": {
                "type": "String"
            }
        },
        "lastModifiedByObjectId": "16bd3f28-b8cb-49cb-98f3-566527c19303",
        "lastPublishTime": "2026-07-10T20:10:30Z"
    }
}




Child:


{
    "name": "pl_dose_src_brz",
    "objectId": "b3b79e02-d56b-4f2a-b68e-289793c8d8d5",
    "properties": {
        "activities": [
            {
                "name": "fe_samms_doseexcuse",
                "type": "ForEach",
                "dependsOn": [
                    {
                        "activity": "flt_child_doseexcuse_sites",
                        "dependencyConditions": [
                            "Succeeded"
                        ]
                    }
                ],
                "typeProperties": {
                    "items": {
                        "value": "@activity('flt_child_doseexcuse_sites').output.value",
                        "type": "Expression"
                    },
                    "isSequential": false,
                    "batchCount": 10,
                    "activities": [
                        {
                            "name": "Dose_excuse_src_to_brz",
                            "type": "Copy",
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
                                        "value": "@concat(\n'IF EXISTS (\n    SELECT 1\n    FROM [', item().DataBaseName, '].sys.tables t\n    JOIN [', item().DataBaseName, '].sys.schemas s\n        ON t.schema_id = s.schema_id\n    WHERE t.name = ''tblDOSE_Excuse''\n      AND s.name = ''dbo''\n)\n\nBEGIN\n\nSELECT\n    ''', item().SiteCode, ''' AS SiteCode,\n    ''', item().DataBaseName, ''' AS SourceDatabase,\n    ''', pipeline().parameters.p_ingest_run_id, ''' AS IngestRunId,\n    GETDATE() AS ExtractedAt,\n\n    ExId,\n    CltID,\n    DtEx,\n    Dtstamp,\n    StrUser,\n    StrExcused,\n\n    CHECKSUM(\n        ExId,\n        CltID,\n        DtEx,\n        Dtstamp,\n        StrUser\n    ) AS RowChkSum\n\nFROM [', item().DataBaseName, '].dbo.tblDOSE_Excuse\n\nEND'\n)",
                                        "type": "Expression"
                                    },
                                    "queryTimeout": "02:00:00",
                                    "partitionOption": "None",
                                    "datasetSettings": {
                                        "annotations": [],
                                        "type": "SqlServerTable",
                                        "schema": [],
                                        "typeProperties": {
                                            "database": "SAMMSGLOBAL"
                                        },
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
                                            "schema": "Dose",
                                            "table": "br_tblDoseExcuse"
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
                        }
                    ]
                }
            },
            {
                "name": "fe_samms_dose",
                "type": "ForEach",
                "dependsOn": [
                    {
                        "activity": "flt_Child_dose_Sites",
                        "dependencyConditions": [
                            "Succeeded"
                        ]
                    }
                ],
                "typeProperties": {
                    "items": {
                        "value": "@activity('flt_Child_dose_Sites').output.value",
                        "type": "Expression"
                    },
                    "batchCount": 10,
                    "activities": [
                        {
                            "name": "Dose_src_to_brz",
                            "type": "Copy",
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
                                        "value": "@concat(\n'IF EXISTS (\n    SELECT 1\n    FROM [', item().DataBaseName, '].sys.tables t\n    JOIN [', item().DataBaseName, '].sys.schemas s\n        ON t.schema_id = s.schema_id\n    WHERE t.name = ''tblDOSE''\n      AND s.name = ''dbo''\n)\n\nBEGIN\n\nSELECT\n    ''', item().SiteCode, ''' AS SiteCode,\n    ''', item().DataBaseName, ''' AS SourceDatabase,\n    ''', pipeline().parameters.p_ingest_run_id, ''' AS IngestRunId,\n    GETDATE() AS ExtractedAt,\n    CONVERT(date, DATEADD(day, -', string(pipeline().parameters.p_lookback_days), ', GETDATE())) AS SourceQueryStartDate,\n    DoseId,\n    CltId,\n    DtMedDate,\n    GuestId,\n    DtDate,\n    Dose,\n    StrUser,\n    BlVoid,\n    StrVoidReason,\n    BlException,\n    Bottletype,\n    Ordernum,\n    ExceptionReason,\n    BlBulk,\n    BlPrepack,\n    Dtgiven,\n    Dtprep,\n    DtVoid,\n    Ppstaff,\n    Exceptiontype,\n    Manualauthdtm,\n    Manualauthuser,\n    Dosenote,\n    Dosesig,\n    InventoryGroup,\n    SiteId,\n    DoseSigImg,\n\n    CHECKSUM(\n        DoseId,\n        CltId,\n        DtMedDate,\n        GuestId,\n        DtDate,\n        Dose,\n        StrUser,\n        BlVoid,\n        StrVoidReason,\n        BlException,\n        Bottletype,\n        Ordernum,\n        ExceptionReason,\n        BlBulk,\n        BlPrepack,\n        Dtgiven,\n        Dtprep,\n        DtVoid,\n        Ppstaff,\n        Exceptiontype,\n        Manualauthdtm,\n        Manualauthuser,\n        Dosenote,\n        InventoryGroup,\n        SiteId\n    ) AS RowChkSum\n\nFROM [', item().DataBaseName, '].dbo.tblDOSE d\n\nWHERE\n    d.dtMedDate >= ''1/1/2020''\n    AND (\n        d.dtMedDate <= CONVERT(date, ''', pipeline().parameters.p_work_date, ''')\n        OR d.dtDate <= CONVERT(date, ''', pipeline().parameters.p_work_date, ''')\n    )\n\nEND'\n)",
                                        "type": "Expression"
                                    },
                                    "queryTimeout": "02:00:00",
                                    "partitionOption": "None",
                                    "datasetSettings": {
                                        "annotations": [],
                                        "type": "SqlServerTable",
                                        "schema": [],
                                        "typeProperties": {
                                            "database": "SAMMSGLOBAL"
                                        },
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
                                            "schema": "Dose",
                                            "table": "br_tblDose"
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
                        }
                    ]
                }
            },
            {
                "name": "set_child_bronze_method_result",
                "type": "SetVariable",
                "dependsOn": [
                    {
                        "activity": "fe_samms_dose",
                        "dependencyConditions": [
                            "Completed"
                        ]
                    },
                    {
                        "activity": "fe_samms_doseexcuse",
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
                                "content": "@concat('{','\"DoseExcuse\":\n{\"status\":\"',if(equals(activity('fe_samms_doseexcuse').Status,'Succeeded'),'SUCCESS','FAILED'),'\",\"failed_stage\":\"',if(equals(activity('fe_samms_doseexcuse').Status,'Succeeded'),'','BR'),'\",\"error_message\":',if(equals(activity('fe_samms_doseexcuse').Status,'Succeeded'),'null',concat('\"',replace(replace(string(coalesce(activity('fe_samms_doseexcuse').error,'DoseExcuse Bronze ForEach failed')),'\"',''''),'\\','\\\\'),'\"')),'},','\"Dose\":\n{\"status\":\"',if(equals(activity('fe_samms_dose').Status,'Succeeded'),'SUCCESS','FAILED'),'\",\"failed_stage\":\"',if(equals(activity('fe_samms_dose').Status,'Succeeded'),'','BR'),'\",\"error_message\":',if(equals(activity('fe_samms_dose').Status,'Succeeded'),'null',concat('\"',replace(replace(string(coalesce(activity('fe_samms_dose').error,'Dose Bronze ForEach failed')),'\"',''''),'\\','\\\\'),'\"')),'}','}')"
                            }
                        }
                    ],
                    "setSystemVariable": true
                }
            },
            {
                "name": "flt_child_doseexcuse_sites",
                "type": "Filter",
                "dependsOn": [],
                "typeProperties": {
                    "items": {
                        "value": "@pipeline().parameters.p_sites",
                        "type": "Expression"
                    },
                    "condition": {
                        "value": "@equals(item().Method, 'DoseExcuse')",
                        "type": "Expression"
                    }
                }
            },
            {
                "name": "flt_Child_dose_Sites",
                "type": "Filter",
                "dependsOn": [],
                "typeProperties": {
                    "items": {
                        "value": "@pipeline().parameters.p_sites",
                        "type": "Expression"
                    },
                    "condition": {
                        "value": "@equals(item().method,'Dose')",
                        "type": "Expression"
                    }
                }
            }
        ],
        "parameters": {
            "p_sites": {
                "type": "array",
                "defaultValue": []
            },
            "p_ingest_run_id": {
                "type": "string",
                "defaultValue": "manual-run"
            },
            "p_lookback_days": {
                "type": "int",
                "defaultValue": 15
            },
            "p_audit_context_json": {
                "type": "string"
            },
            "p_work_date": {
                "type": "string",
                "defaultValue": "2026-07-06"
            }
        },
        "variables": {
            "v_bronze_method_results_json": {
                "type": "String"
            }
        },
        "lastModifiedByObjectId": "16bd3f28-b8cb-49cb-98f3-566527c19303",
        "lastPublishTime": "2026-07-09T20:31:24Z"
    }
}



