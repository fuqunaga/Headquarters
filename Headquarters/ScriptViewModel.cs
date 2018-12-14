using NetTools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Headquarters
{
    class ScriptViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;


        public string Header => script.name;
        public ObservableCollection<Parameter> Parameters { get; protected set; }

        protected Script script { get; set; }

        public bool IsRunning => tasks.Any(t => !t.IsCompleted);


        string resultText_ = "";
        public string ResultText { get => resultText_;
            protected set
            {
                resultText_ = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ResultText)));
            }
        }


        List<Task> tasks = new List<Task>();
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

        public Task Run(List<IPParams> ipParams)
        {
            Task ret = null;
            if (IsRunning) return ret;

            ipParams.ForEach(param =>
            {
                IPAddressRange.TryParse(param.ipStr, out IPAddressRange range);

                var ipStrList = range?.AsEnumerable().Select(ip => ip.ToString()).ToList() ?? (new[] { param.ipStr }).ToList();

                 var paramIsValid = true;
                 if (ipStrList.Count > 100)
                 {
                     var result = MessageBox.Show($"IP[{param.ipStr}] means {ipStrList.Count} targets.\n Continue?", "Many targets", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                     paramIsValid = (result == MessageBoxResult.OK);
                 }

                 if (paramIsValid)
                 {
                    ResultText = "";

                    script.Load();

                    cancelTokenSource = new CancellationTokenSource();
                    var cancelToken = cancelTokenSource.Token;
                    var parameters = Parameters.Concat(new[]
                    {
                        ParameterManager.UserName,
                        ParameterManager.UserPassword,
                    })
                    .GroupBy(p => p.Name)
                    .Select(g => g.First())
                    .ToDictionary(p => p.Name, p => (object)p.Get(param));

                    tasks = ipStrList.Select(ipStr =>
                    {
                        return Task.Run(() =>
                        {
                            var result = script.Run(ipStr, parameters, cancelToken);
                            UpdateText(ipStr, result);
                        });
                    })
                    .ToList();

                    ret = Task.Run(() => Task.WaitAll(tasks.ToArray()));
                 }
             });

            return ret;
        }

        internal void Stop()
        {
            cancelTokenSource.Cancel();
        }


        void UpdateText(string ipStr, PowerShellScript.Result result)
        {
            var prefix = result.IsSuccessed ? "✔" : "⚠";
            var str =  prefix + ipStr + ":" + (result.canceled ? "canceled" : "") + "\n";
            str += ResultToString(result) +"\n\n";

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
