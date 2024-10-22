using System;
using System.Collections.Generic;

namespace Headquarters;

/// <summary>
/// Globalパラメータの定義をまとめるクラス
/// </summary>
public static class GlobalParameter
{
    private const string UserNameAndPasswordDescription = """
                                                          リモートPCに接続するためのユーザ名とパスワードを設定します
                                                          不要な場合は空欄にしてください

                                                          複数のリモートPCで別々のユーザ名とパスワードを使用する場合は、
                                                          IPListに"UserName"と"UserPassword"という列を追加して指定ください

                                                          "UserPassword"は平文で"./setting.json"に保存されます
                                                          取扱いにご注意ください
                                                          """;

    public const string UserNameParameterName = "UserName";
    public const string UserPasswordParameterName = "UserPassword";

    private const string ConfirmationProcessCountDescription = "これ以上のプロセス数がある場合、実行前に確認ダイアログを表示します";
    public const string ConfirmationProcessCountParameterName = "ConfirmationProcessCount";


    public static int ConfirmationProcessCount => int.Parse(ParameterSet?.Get(ConfirmationProcessCountParameterName) ?? "100");
    public static ParameterSet? ParameterSet { get; set; }
    
    
    public static IEnumerable<ParameterSetViewModel> CreateParameterSetViewModels()
    {
        if (ParameterSet is null)
        {
            throw new InvalidOperationException("ParameterSetが設定されていません");
        }

        yield return CreateUserNameAndPasswordParameterSetViewModel(ParameterSet);
        yield return CreateConfirmationProcessCountParameterSetViewModel(ParameterSet);
    }

    private static ParameterSetViewModel CreateUserNameAndPasswordParameterSetViewModel(ParameterSet parameterSet)
    {
        var parameterSetViewModel = new ParameterSetViewModel(UserNameAndPasswordDescription);
        parameterSetViewModel.Parameters.Add(new ParameterViewModel(UserNameParameterName, parameterSet));
        parameterSetViewModel.Parameters.Add(new ParameterViewModel(UserPasswordParameterName, parameterSet));

        return parameterSetViewModel;
    }
    
    private static ParameterSetViewModel CreateConfirmationProcessCountParameterSetViewModel(ParameterSet parameterSet)
    {
        var parameterSetViewModel = new ParameterSetViewModel(ConfirmationProcessCountDescription);
        parameterSetViewModel.Parameters.Add(new ParameterViewModel(ConfirmationProcessCountParameterName, parameterSet));

        return parameterSetViewModel;
    }
}