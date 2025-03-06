<#
.SYNOPSIS
TaskContextのSharedDictionaryを利用したマルチスレッド対応のオブジェクトプール

.PARAMETER PoolId
Poolを識別するための任意のID
#>

class Pool {
    [string]$PoolId
    [Headquarters.TaskContext]$TaskContext

    Pool([string]$PoolId, [Headquarters.TaskContext]$TaskContext) {
        $this.TaskContext = $TaskContext
        $this.PoolId = $PoolId
    }

    [System.Collections.Concurrent.ConcurrentBag[object]]GetBag()
    {
        $sharedDictionary = $this.TaskContext.SharedDictionary
        $bag = $null
        $sharedDictionary.TryGetValue($this.PoolId, [ref]$bag);
        return $bag
    }


    [System.Collections.Concurrent.ConcurrentBag[object]]GetOrCreateBag()
    {
        $bag = $this.GetBag()
        if($null -eq $bag) {
            $sharedDictionary = $this.TaskContext.SharedDictionary
            $bag = $sharedDictionary.GetOrAdd($this.PoolId, [System.Collections.Concurrent.ConcurrentBag[object]]::new())
        }

        return $bag;
    }

    [object]TryGetObject()
    {
        $bag = $this.GetBag()
        if ( $null -eq $bag) {
            return $null
        }

        $obj = $null;
        if (-not $bag.TryTake([ref]$obj)) {
            return $null
        }

        return $obj
    }

    [void]SetObject([object]$Object)
    {
        $bag = $this.GetOrCreateBag()
        $bag.Add($Object)
    }
}

function Get-PoolFromTaskContext() 
{
    [CmdletBinding()]
    param(
        [ValidateNotNullOrEmpty()]
        [string]$PoolId,
        [ValidateNotNull()]
        $TaskContext
    )

    return [Pool]::new($PoolId, $TaskContext)
}