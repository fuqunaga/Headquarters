@{
    ModuleVersion = '0.0'
    RootModule = 'TaskContextUtility.psm1'
    FunctionsToExport = @(
        'New-PSSessionFromTaskContext', 
        'Get-PoolFromTaskContext'
    )
}