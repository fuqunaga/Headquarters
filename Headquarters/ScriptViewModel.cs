using MaterialDesignThemes.Wpf.Transitions;
using NetTools;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
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

                    tasks = ipStrList.Select(ipStr =>
                    {
                        var parameters = Parameters.ToDictionary(p => p.Name, p => (object)p.Get(param));
                        return Task.Run(() => script.Run(ipStr, parameters)).ContinueWith(task => UpdateText(ipStr, task.Result));
                    })
                    .ToList();

                    ret = Task.Run(() => Task.WaitAll(tasks.ToArray()));
                 }
             });

            return ret;
        }

        internal void Stop()
        {
            if ( IsRunning)
            {
            }
        }


        void UpdateText(string ipStr, PowerShellScript.Result result)
        {
            var str = (result.IsSuccessed ? "✔" : "⚠") + $"{ipStr}:\n";
            str += ResultToString(result) +"\n\n";

            lock (this)
            {
                ResultText += str;
            }
        }



        string ResultToString(PowerShellScript.Result result)
        {
            return $"{ string.Join("\n", result.objs)}\n{ string.Join("\n", result.errors)}";
        }
    }
}
