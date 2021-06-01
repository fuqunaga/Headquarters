using NetTools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Headquarters
{
    class ScriptViewModel : INotifyPropertyChanged
    {
        #region Type Define

        public static class OwnParameter
        {
            public const string MaxTaskNum = "MaxTaskNum";
        }

        public class OutputData
        {
            public string name;
            public PSInvocationStateInfo info;
            public PowerShellScript.Result result;

            public override string ToString()
            {
                var prefix = (result == null) ? "" : (result.IsSuccessed ? "✔" : "⚠");
                var label = $"{prefix} {name}: {info?.State}";
                var resultStr = GetResultString();

                var ret = label;
                if (resultStr.Any()) ret += "\n" + resultStr;

                return ret;
            }

            string GetResultString()
            {
                string ToStr<T>(IEnumerable<T> collection)
                {
                    string str = "";
                    if (collection?.Any() ?? false)
                    {
                        str = string.Join("\n ", collection.Select(elem => $" {elem.ToString()}")) + "\n";
                    }
                    return str;
                }

                var ret = "";
                if (result != null)
                {
                    var objStr = ToStr(result.objs);
                    var errStr = ToStr(result.errors);

                    ret = objStr
                        + ((objStr.Any() && errStr.Any()) ? "\n" : "")
                        + errStr;
                }

                return ret;
            }
        }

        #endregion


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

        public int MaxTaskNum
        {
            get
            {
                var ret = GetOwnParam(OwnParameter.MaxTaskNum);
                return (ret != null) ? Convert.ToInt32(ret) : 100;
            }
            set
            {
                if (MaxTaskNum != value)
                {
                    SetOwnParam(OwnParameter.MaxTaskNum, value);
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MaxTaskNum)));
                }
            }
        }

        #endregion


        protected Script script { get; set; }

        string ToOwnParamName(string name) => script.name + "." + name;

        // parameter by script
        object GetOwnParam(string paramName) => ParameterManager.Instance.Get(ToOwnParamName(paramName));

        void SetOwnParam(string paramName, object value) => ParameterManager.Instance.Set(ToOwnParamName(paramName), value);

        CancellationTokenSource cancelTokenSource;
        List<OutputData> outputDatas = new List<OutputData>();

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

        void ClearOutput()
        {
            outputDatas.Clear();
            ResultText = "";
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
                var paramDicOrig = parameters.ToDictionary(p => p.Name, p => (object)p.Get(ipParams));
                return ipStrList.Select(ip => new { ip, paramDic = new Dictionary<string, object>(paramDicOrig) });
            })
            .ToList();


            ClearOutput();
            script.Load();

            cancelTokenSource = new CancellationTokenSource();
            var cancelToken = cancelTokenSource.Token;

            var count = Math.Min(ipAndParams.Count(), MaxTaskNum);
            var rsp = RunspaceFactory.CreateRunspacePool(1, count);
            rsp.Open();

            var tasks = ipAndParams.Select(ipAndParam =>
            {
                var ip = ipAndParam.ip;
                var data = new OutputData()
                {
                    name = ip
                };

                outputDatas.Add(data);


                return Task.Run(() =>
                {
                    var param = new PowerShellScript.InvokeParameter()
                    {
                        rsp = rsp,
                        parameters = ipAndParam.paramDic,
                        cancelToken = cancelToken,
                        invocationStateChanged = (_, e) =>
                        {
                            data.info = e.InvocationStateInfo;
                            UpdateOutput();
                        }
                    };

                    var result = script.Run(ipAndParam.ip, param);
                    data.result = result;
                    UpdateOutput();
                });
            });

            return Task.WhenAll(tasks)
                .ContinueWith(_ => rsp.Dispose());
        }

        internal void Stop()
        {
            cancelTokenSource?.Cancel();
        }

        void UpdateOutput()
        {
            var str = string.Join("\n", outputDatas.Select(data => data.ToString()));

            lock (this)
            {
                ResultText = str;
            }
        }
    }
}
