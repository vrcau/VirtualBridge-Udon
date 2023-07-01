using System;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Data;
using VRC.SDK3.StringLoading;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace VirtualBridge
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VirtualBridgeClient : UdonSharpBehaviour
    {
        public Text debugOutput;

        private bool _lastRequestFinish = true;
        private readonly VRCUrl _url = new VRCUrl("http://localhost:6547/messages/pull");

        public float updateInterval = 2f;
        private float _time;

        private string[] _registeredType = { };
        private VirtualBridgeEventReceiver[] _registeredReceiver = { };

        private readonly DataDictionary _dictionary = new DataDictionary()
        {
            { "version", "1" },
            { "content", "" },
            { "type", "unknown" },
            { "timestamp", new DataToken(DateTimeOffset.MinValue) }
        };

        [PublicAPI]
        public void RegisterReceiver(string type, VirtualBridgeEventReceiver receiver)
        {
            _registeredType = Add(_registeredType, type);
            _registeredReceiver = Add(_registeredReceiver, receiver);
        }

        [PublicAPI]
        public void SendData(string type, string data)
        {
            _dictionary["content"] = data;
            _dictionary["type"] = type;
            if (VRCJson.TrySerializeToJson(_dictionary, JsonExportType.Minify, out var result))
            {
                Debug.Log($"[vbdt]{result}");
            }
        }

        private void Update()
        {
            if (Time.time - _time < updateInterval) return;
            _time = Time.time;

            if (!_lastRequestFinish) return;

            // ReSharper disable once SuspiciousTypeConversion.Global
            VRCStringDownloader.LoadUrl(_url, (IUdonEventReceiver)this);
            _lastRequestFinish = false;
        }

        public override void OnStringLoadError(IVRCStringDownload result)
        {
            _lastRequestFinish = true;
        }

        public override void OnStringLoadSuccess(IVRCStringDownload result)
        {
            _lastRequestFinish = true;

            if (!VRCJson.TryDeserializeFromJson(result.Result, out var jsonData)) return;

            foreach (var item in jsonData.DataList.ToArray())
            {
                var version = item.DataDictionary["version"].String;
                var timestamp = DateTimeOffset.Parse(item.DataDictionary["timestamp"].String);
                var data = item.DataDictionary["data"].String;
                var type = item.DataDictionary["type"].String;

                if (debugOutput != null)
                    debugOutput.text += $"version: {version} type: {type} data: {data} time: {timestamp}\n";

                for (var i = 0; i < _registeredType.Length; i++)
                {
                    if (_registeredType[i] == type)
                        _registeredReceiver[i].OnVirtualBridgeDataReceived(data);
                }
            }
        }

        private static T[] Add<T>(T[] array, T item)
        {
            var tempArray = new T[array.Length + 1];
            array.CopyTo(tempArray, 0);

            tempArray[tempArray.Length - 1] = item;
            return tempArray;
        }
    }
}