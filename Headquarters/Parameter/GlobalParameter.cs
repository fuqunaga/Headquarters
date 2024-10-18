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
                                                          """;
    
    public const string UserNameParameterName = "UserName";
    public const string UserPasswordParameterName = "UserPassword";
    
    
    public static IEnumerable<ParameterSetViewModel> CreateParameterSetViewModels(ParameterSet parameterSet)
    {
        yield return CreateUserNameAndPasswordParameterSetViewModel(parameterSet);
    }
    
    private static ParameterSetViewModel CreateUserNameAndPasswordParameterSetViewModel(ParameterSet parameterSet)
    {
        var parameterSetViewModel = new ParameterSetViewModel(UserNameAndPasswordDescription);
        parameterSetViewModel.Parameters.Add(new ParameterViewModel(UserNameParameterName, parameterSet));
        parameterSetViewModel.Parameters.Add(new ParameterViewModel(UserPasswordParameterName, parameterSet));
        
        return parameterSetViewModel;
    }
}