using MaterialDesignThemes.Wpf.Transitions;
using NetTools;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Headquarters
{
    class ScriptViewModel : INotifyPropertyChanged
    {
        public string Header => script.name;
        public List<Parameter> Parameters { get; protected set; }

        public Script script { get; protected set; }

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

        public event PropertyChangedEventHandler PropertyChanged;



        public ScriptViewModel(Script script)
        {
            this.script = script;

            Parameters = script.paramNames.Select(p => new Parameter(p)).ToList();
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

                    var cancelToken = cancelTokenSource.Token;

                    tasks = ipStrList.Select(ipStr =>
                    {
                        var parameters = Parameters.ToDictionary(p => p.Name, p => (object)p.Get(param));
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
            var strs = ToStrings(result.errors);
            return $"{ string.Join("\n", result.objs)}\n{ string.Join("\n", strs)}";
        }

        IEnumerable<string> ToStrings<T>(List<T> collection)
        {
            return collection.Select(item => item.ToString());
        }
    }
}
