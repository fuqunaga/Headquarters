using NetTools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Management.Automation.Runspaces;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Headquarters
{
    class ScriptViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        #region Binding Properties

        public string Header => script.name;
        public ObservableCollection<Parameter> Parameters { get; protected set; }

        string resultText_ = "";
        public string ResultText
        {
            get => resultText_;
            protected set
            {
                resultText_ = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ResultText)));
            }
        }

        int MaxTaskNum_ = 100;
        public int MaxTaskNum
        {
            get => MaxTaskNum_;
            set
            {
                MaxTaskNum_ = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MaxTaskNum)));
            }
        }

        #endregion

        protected Script script { get; set; }

        CancellationTokenSource cancelTokenSource = new CancellationTokenSource();


        public ScriptViewModel(Script script)
        {
            this.script = script;
        }

        public void Load()
        {
            script.Load();
            Parameters = new ObservableCollection<Parameter>(script.paramNames.Select(p => new Parameter(p)));

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Parameters)));
        }

        public Task Run(IReadOnlyCollection<IPParams> ipParamsList)
        {
            var parameters = Parameters.Concat(new[]
            {
                ParameterManager.UserName,
                ParameterManager.UserPassword,
            })
            .GroupBy(p => p.Name)
            .Select(g => g.First())
            .ToList();

            // Check so many IP on ipParamsList
            var soManyIp = ipParamsList.Select(ipParams => IPAddressRange.TryParse(ipParams.ipStr, out var range)
                ? new { ipParams.ipStr, count = range.AsEnumerable().Count() }
                : null
            )
            .Where(pair => pair != null)
            .FirstOrDefault(pair => pair.count > 100);

            if (soManyIp != null)
            {
                var result = MessageBox.Show($"IP[{soManyIp.ipStr}] means {soManyIp.count} targets.\n Continue?", "Many targets", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                if (result != MessageBoxResult.OK) return null;
            }


            var ipAndParams = ipParamsList.SelectMany(ipParams =>
            {
                IPAddressRange.TryParse(ipParams.ipStr, out var range);
                var ipStrList = range?.AsEnumerable().Select(ip => ip.ToString()) ?? (new[] { ipParams.ipStr });
                var paramDic = parameters.ToDictionary(p => p.Name, p => (object)p.Get(ipParams));
                return ipStrList.Select(ip => new { ip, paramDic });
            })
            .ToList();


            ResultText = "";
            script.Load();

            var cancelToken = cancelTokenSource.Token;

            var count = Math.Min(ipAndParams.Count(), MaxTaskNum);
            var rsp = RunspaceFactory.CreateRunspacePool(1, count);
            rsp.Open();

            var tasks = ipAndParams.Select(ipAndParam =>
            {
                return Task.Run(() =>
                {
                    var result = script.Run(rsp, ipAndParam.ip, ipAndParam.paramDic, cancelToken);
                    UpdateText(ipAndParam.ip, result);
                });
            });

            return Task.WhenAll(tasks)
                .ContinueWith(_ => rsp.Dispose());
        }

        internal void Stop()
        {
            cancelTokenSource.Cancel();
        }


        void UpdateText(string ipStr, PowerShellScript.Result result)
        {
            var prefix = result.IsSuccessed ? "✔" : "⚠";
            var str = prefix + ipStr + ":" + (result.canceled ? "canceled" : "") + "\n";
            str += ResultToString(result) + "\n\n";

            lock (this)
            {
                ResultText += str;
            }
        }

        string ResultToString(PowerShellScript.Result result)
        {
            var objStrs = ToStrings(result.objs);
            var errStrs = ToStrings(result.errors);
            return $"{ string.Join("\n", objStrs)}\n{ string.Join("\n", errStrs)}";
        }

        IEnumerable<string> ToStrings<T>(IEnumerable<T> collection)
        {
            return collection?.Select(item => item.ToString()) ?? new string[0];
        }
    }
}
