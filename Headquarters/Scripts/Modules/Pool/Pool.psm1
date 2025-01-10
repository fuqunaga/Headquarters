<#
.SYNOPSIS
タスク間でobjectを共有するための機能を提供します

.DESCRIPTION
TaskContext.SharedDictionaryを使用してタスク間でobjectを共有します

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

    [System.Collections.Concurrent.ConcurrentQueue[object]]GetQueue()
    {
        $sharedDictionary = $this.TaskContext.SharedDictionary
        $queue = $null
        $sharedDictionary.TryGetValue($this.PoolId, [ref]$queue);
        return $queue
    }


    [System.Collections.Concurrent.ConcurrentQueue[object]]GetOrCreateQueue()
    {
        $queue = $this.GetQueue()
        if($null -eq $queue) {
            $sharedDictionary = $this.TaskContext.SharedDictionary
            $queue = $sharedDictionary.GetOrAdd($this.PoolId, [System.Collections.Concurrent.ConcurrentQueue[object]]::new())
        }

        return $queue;
    }

    [object]TryGetObject()
    {
        $queue = $this.GetQueue()
        if ( $null -eq $queue) {
            return $null
        }

        $obj = $null;
        if (-not $queue.TryDequeue([ref]$obj)) {
            return $null
        }

        return $obj
    }

    [void]SetObject([object]$Object)
    {
        $queue = $this.GetOrCreateQueue()
        $queue.Enqueue($Object)
    }
}

function Get-Pool() {
    param(
        [ValidateNotNullOrEmpty()]
        [string]$PoolId,
        [ValidateNotNull()]
        $TaskContext
    )

    return [Pool]::new($PoolId, $TaskContext)
}