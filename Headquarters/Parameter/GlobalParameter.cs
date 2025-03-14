﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Headquarters;

/// <summary>
/// Globalパラメータの定義をまとめるクラス
/// </summary>
public static class GlobalParameter
{
    public const string UserNameParameterName = "UserName";
    public const string UserPasswordParameterName = "UserPassword";
    public const string ShowConfirmationDialogOnExecuteParameterName = "ShowConfirmationDialogOnExecute";

    public const string UserNameAndPasswordDescription = """
                                                         リモートPCのアカウント
                                                         不要な場合は空欄にしてください
                                                         複数のリモートPCで別々のアカウントを指定する場合はIP Listに"UserName","UserPassword"という列を追加して指定ください
                                                         平文で".\HeadquartersData/setting.json"に保存されます。取扱いにご注意ください
                                                         """;


    private static readonly IReadOnlyDictionary<string, (ParameterDefinition difinition, string help)>
        ParameterDefinitionAndHelps = new Dictionary<string, (ParameterDefinition difinition, string help)>
        {
            {
                UserNameParameterName,
                (
                    new ParameterDefinition(UserNameParameterName),
                    "ユーザー名"
                )
            },
            {
                UserPasswordParameterName,
                (
                    new ParameterDefinition(UserPasswordParameterName),
                    "パスワード"
                )
            },
            {
                ShowConfirmationDialogOnExecuteParameterName,
                (
                    new ParameterDefinition(ShowConfirmationDialogOnExecuteParameterName)
                    {
                        ConstraintType = typeof(bool),
                        DefaultValue = true,
                    },
                    "タスク実行前に確認ダイアログを表示する"
                )
            }
        };

    public static string UserName => ParameterSet?.Get(UserNameParameterName) ?? "";
    public static string UserPassword => ParameterSet?.Get(UserPasswordParameterName) ?? "";
    public static bool ShowConfirmationDialogOnExecute
    {
        get
        {
            // パラメータが存在しない場合はtrueを返す
            var stringValue = ParameterSet?.Get(ShowConfirmationDialogOnExecuteParameterName);
            if(stringValue is null || !bool.TryParse(stringValue, out var result))
            {
                return true;
            }
            
            return  result;
        }
    }
    
    public static ParameterSet? ParameterSet { get; private set; }

    public static void SetParameterSet(Dictionary<string, string>  parameterTable)
    {
        if (!parameterTable.ContainsKey(ShowConfirmationDialogOnExecuteParameterName))
        {
            parameterTable[ShowConfirmationDialogOnExecuteParameterName] = "true";
        }
        
        ParameterSet = new ParameterSet(parameterTable);
    }

    public static ParameterInputFieldViewModel CreateParameterInputFieldViewModel(string parameterName)
    {
        if (ParameterSet is null)
        {
            throw new InvalidOperationException("ParameterSetが設定されていません");
        }

        if (!ParameterDefinitionAndHelps.ContainsKey(parameterName))
        {
            throw new ArgumentException($"存在しないパラメータ名です: {parameterName}");
        }

        return new ParameterInputFieldViewModel(
            ParameterDefinitionAndHelps[parameterName].difinition,
            ParameterDefinitionAndHelps[parameterName].help,
            ParameterSet
        );
    }
}