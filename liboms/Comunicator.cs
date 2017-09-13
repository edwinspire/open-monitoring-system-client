//
//  MyClass.cs
//
//  Author:
//       Edwin De La Cruz <edwinspire@gmail.com>
//
//  Copyright (c) 2017 edwinspire
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System.Threading;
using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Security;
using System.Net.Sockets;
using Quobject.SocketIoClientDotNet.Client;
using System.Data;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace OpenMonitoringSystem
{



	namespace Client{

		public struct CommunicatorConfig{
			public string Server;
			public string ServerBackup;
			public int Id;
			public string Pwd;
			public bool secure;
		}

		public struct Device{
			//public string Server;
			//public string ServerBackup;
			public string Key;
			public string ID;
			//public bool secure;
		}



		/*
		public struct ComunicatorParam{
			public string Server;
			public string ServerBackup;
			public int idequipment;
			public string DeviceKey;
			public string service;
			public bool secure;
		}
		*/


		public enum DeliverStatus{
			Ok = 0,
			Error = 2,
			Duplicate = 1,
			Unknow = -1,
		}


		public struct WSReturnData{
			public string idqueue;
			public int? idreg;
			public string message;
			public DeliverStatus deliver_status;
		}

		public struct WSSReturn{
			public string Service;
			public string DeviceKey;
			public string Message;
			public System.Collections.Generic.List<WSReturnData> Return;
		}

		public struct WSService{
			public string token;
			//public string Service;
			//public DateTime DateQueue = DateTime.Now;
			public string DeviceKey;
			public System.Collections.Generic.Dictionary<string, List<string>> datas;
		}

//		public struct QueueStatus{
//			public DateTime Datetime;
//			public string idQueue;
//		}

		public class Comunicator:BaseObject{

			public CommunicatorConfig CConfig = new CommunicatorConfig();
			//public override string ObjectName { get { return "Comunicator"; } }
			public Quobject.SocketIoClientDotNet.Client.Socket socket;
			private string token = "qwerty1234";
			public event EventHandler OnReady;
			public ShippingQueue Queue = new ShippingQueue();
			public Device CDevice = new Device();
			//public CommunicatorConfig Config = new CommunicatorConfig();

			public Comunicator(){

				this.Queue.ResetMessageToSend ();
				this.watchFiles ();
				//this.CParam.Server = "http://localhost";
				//this.Param.idequipment = 0;
				//this.CParam.DeviceKey = "abcdefghijklmnopqrstuvwzyz";
				//this.Param.service = "events.receiver";
				//this.CParam = getLocalObject <ConnectionParameters>(); //this.Param.getLocalObject <ComunicatorParam>();
			}

			private void watchFiles()
			{
				FileSystemWatcher watcher = new FileSystemWatcher();
				watcher.InternalBufferSize = 16385;
				watcher.IncludeSubdirectories = true;
				//watcher.EnableRaisingEvents = true;
				watcher.Path = this.Queue.queue_dir();
				watcher.NotifyFilter = NotifyFilters.CreationTime;
				watcher.Filter = "*.json";
				watcher.Created += new FileSystemEventHandler(OnCreated);
				watcher.EnableRaisingEvents = true;
			}

			private void OnCreated(object source, FileSystemEventArgs e)
			{
				//Console.WriteLine (e.FullPath);
				Console.WriteLine (e.ChangeType.ToString());
//				Console.WriteLine (e.Name);
				//Console.WriteLine ("--> "+Path.GetDirectoryName(e.FullPath));
				if (this.token.Length > 10) {
                    //this.Queue.ResetMessageToSend();
					//this.EmitMessage (this.Queue.ToSend ());	
				} else {
					Console.WriteLine ("No nay un token valido");
				}
			}

			private void EmitOnReady()
			{
				EventHandler eh = OnReady;

				if(eh != null)
				{
					Console.WriteLine ("Token: "+this.token);
					eh(this, EventArgs.Empty);
				}
			}


			private void QueueAsSent(WSSReturn Return){

				//Console.WriteLine (Return.Return.Count.ToString()+" mensajes para eliminar de la cola.");
				foreach(var q in Return.Return){
					if (q.deliver_status == DeliverStatus.Ok) {
						this.Queue.ChangeStatusMessage(Return.Service, Return.DeviceKey, q.idqueue, QueueMessageStatus.Sending, QueueMessageStatus.Sent);
					} else {
						this.Queue.ChangeStatusMessage(Return.Service, Return.DeviceKey, q.idqueue, QueueMessageStatus.Sending, QueueMessageStatus.Fail);
					}
					/*if(q.idreg != null && q.idreg > 0){
						this.Queue.ChangeStatusMessage(Return.Service, Return.DeviceKey, q.idqueue, QueueMessageStatus.Sending, QueueMessageStatus.Sent);
					}else if(q.idreg == null && q.idreg < 1 && q.retry == false){
						this.Queue.ChangeStatusMessage(Return.Service, Return.DeviceKey, q.idqueue, QueueMessageStatus.Sending, QueueMessageStatus.SentNotRetry);					
					}
					*/
				}

				Console.WriteLine ("Ok");
			}

		

			public void SendByWS(){
				if (this.token.Length > 10) {
					this.EmitMessage (this.Queue.ToSend ());
				} else {
					Console.WriteLine ("Token "+this.token+" no valido");
				}
			}
				

			public void SendByHTTP(){
				//this.SendByHTTP(this.queue.getQueue ());
			}

			/*
			public void ByHTTP(List<Agent> agents){

				this.getLocalParams ();

				foreach(var a in agents){
					a.OnEvents += delegate(object o, OMSEventsList ev)  
					{  
						foreach(var e in this.SendEvent(ev.events)){
							Console.WriteLine(e);
						}
					};  
				}

				foreach(var a in agents){
					a.StartInterval ();
				}
				Console.WriteLine ("Termina de inicializar");
			}
			*/
			/*
			public void QueueEventsAdd(List<EventData> e){
				this.queue.Add(e);
			}
			*/
			private void connect(List<BaseBot> agents){

				this.getLocalParams ();
				var host = "ws://" + this.CConfig.Server;
				var options = new IO.Options() { IgnoreServerCertificateValidation = true, AutoConnect = true, ForceNew = true };
				//this.socket = IO.Socket("http://www.farmaenlace.com:8093");
				if(this.CConfig.secure == true){
					host = "wss://" + this.CConfig.Server;
				}


				this.socket = IO.Socket(host, options);

				foreach(var a in agents){
					a.StartInterval ();
				}

				/*
				foreach(var a in agents){
					a.OnEvents += delegate(object o, OMSEventsList ev)  
					{  
						Console.WriteLine("- Evento recibido del cliente "+ev.events.Count.ToString());

						foreach(var e in ev.events){
							this.queue.Add(new QueueItem("events", e));	
						}
							
					};  
				}
				*/



				socket.On("wssreturn", (received) =>
					{
						var rec = received.ToString();
							var R = DeserializeObject<WSSReturn>(rec);

						if(!Object.ReferenceEquals(R, null)){
							Console.WriteLine("< El servidor ha devuelto "+R.Return.Count.ToString()+" mensajes. "+R.Message);
							QueueAsSent(R);
						}

						//socket.Disconnect();
					});

				socket.On("connection", (e) =>
					{
						Console.WriteLine(this.CConfig.Server);
						Console.WriteLine(e);
						this.token = "";
						socket.Emit("clogin", SerializeObject<CommunicatorConfig>(this.CConfig));
					});

				socket.On("clogged", (e) =>
					{
						this.token = e.ToString();
						Console.WriteLine("Se ha logueado...");
						this.SendByWS ();
					});

				socket.On("token_expired", (e) =>
					{
						this.token = "";
						socket.Emit("clogin", SerializeObject<CommunicatorConfig>(this.CConfig));
					});


				socket.On(Quobject.SocketIoClientDotNet.Client.Socket.EVENT_RECONNECT, () =>
					{
						Console.WriteLine("EVENT_RECONNECT");
						this.token = "";
						socket.Emit("clogin", SerializeObject<CommunicatorConfig>(this.CConfig));
					});

				socket.On(Quobject.SocketIoClientDotNet.Client.Socket.EVENT_MESSAGE, () =>
					{
						Console.WriteLine("EVENT_MESSAGE");
					});

				socket.On(Quobject.SocketIoClientDotNet.Client.Socket.EVENT_CONNECT_ERROR, (e) =>
					{
						Console.WriteLine("EVENT_CONNECT_ERROR "+e.ToString());
						this.token = "";
					});

				socket.On(Quobject.SocketIoClientDotNet.Client.Socket.EVENT_DISCONNECT, (e) =>
					{
						Console.WriteLine("EVENT_DISCONNECT "+e.ToString());
						this.token = "";
						//socket.Connect();
					});
				
				socket.On(Quobject.SocketIoClientDotNet.Client.Socket.EVENT_ERROR, (e) =>
					{
						Console.WriteLine("EVENT_ERROR "+e.ToString());
					});
			}

			public void  Connect(List<BaseBot> agents){
				Thread t = new Thread(()=>connect(agents));
				t.Start();
			}

			public void Connect(){
				this.Connect(new List<BaseBot>());
			}
				


			public override void getLocalParams(){

			//	public CommunicatorConfig CConfig = new CommunicatorConfig();

				//this.Queue.Param = getAnyLocalObject <QueueParam>("QueueParam", this.BaseConfig.ConfigPath);
				this.CConfig = getConfigObject <CommunicatorConfig>("CommunicatorConfig"); 
			
				if (string.IsNullOrEmpty(this.CConfig.Server))
				{
					throw new System.ArgumentException("Cannot be null or empty. Config Path: "+this.BaseConfig.ConfigPath, "Server");
				}


				if (this.CConfig.Id <= 0)
				{
					throw new System.ArgumentException("Cannot be null or empty. Config Path: "+this.BaseConfig.ConfigPath, "Id");
				}


				//this.Param.getLocalObject <ComunicatorParam>();
			}

			public static string GetLocalIPAddress()
			{
				var host = Dns.GetHostEntry(Dns.GetHostName());
				foreach (var ip in host.AddressList)
				{
					if (ip.AddressFamily == AddressFamily.InterNetwork)
					{
						return ip.ToString();
					}
				}
				throw new Exception("Local IP Address Not Found!");
			}

//			public static string Post(string uri, NameValueCollection pairs)
//			{
//				string response = "";
//				try{
//					using (WebClient client = new WebClient())
//					{
//						response = System.Text.Encoding.UTF8.GetString(client.UploadValues(uri, pairs));
//					}
//				}
//				catch(Exception e){
//					Console.WriteLine (e.Message);
//					return null;
//				}
//
//				return response;
//			}

//			public List<string> SendMessage(System.Collections.Generic.List<JObject> ev)
//			{
//				var R = new List<string>();
//				var datas = JsonConvert.SerializeObject (ev);
//				var server = "http://"+this.CConfig.Server + "/service/events/receiver/w/" + Md5(datas) + "/" + ((int)DateTime.Now.Ticks).ToString () + "/"+ ((int)DateTime.Now.Ticks).ToString () + "/"+ev.Count.ToString();
//
//				if(ev.Count > 0){
//					var	txt = Post(server, new NameValueCollection()
//						{
//							{ "DeviceKey", this.CDevice.Key},
//							{ "list_events", datas}
//						});
//
//					R = getEventsReceived (txt);
//
//				}
//				return R;
//			}

//			private List<string> getEventsReceived(string response){
//				string pattern = @"\[\{\""fun_receiver_json\"":(.*?)\}\]";
//				var R = new List<string> ();
//				Regex rex = new Regex(pattern);
//				var txt = response.Replace ("\n", "").Replace (" ", "");
//				MatchCollection matches = rex.Matches(txt);
//
//				if (matches.Count > 0) {
//					foreach (Match match in matches) {
//						R = DeserializeObject<List<string>> (match.Groups [1].Value);
//					}
//				}
//				return R;
//			}

			private List<List<JObject>> MessageGroup(List<JObject> m){
				return m
					.GroupBy (u => u.GetValue ("DeviceKey"))
					//.OrderBy (u => "DateQueue")
					.Select(grp => grp.ToList())
					.ToList();
			}

            private Dictionary<string, WSService> GroupSEmit(List<JObject> m) {

                var devices = new System.Collections.Generic.Dictionary<string, WSService>();


                try
                {


                    foreach (var a in MessageGroup(m))
                {

                    foreach (var b in a)
                    {
                        var service = b.GetValue("QueueService").ToString();
                        var key = b.GetValue("DeviceKey").ToString();
                        var idQueue = b.GetValue("idQueue").ToString();
                        var datas = setIdQueueToData(idQueue, b.GetValue("Datas").ToString());

                        var sk = key + "-" + service;

                        if (!devices.ContainsKey(sk))
                        {
                            var ws = new WSService();
                            ws.datas = new Dictionary<string, List<string>>();
                            ws.DeviceKey = key;
                            ws.token = this.token;
                            var lism = new List<string>();

                            lism.Add(datas);
                            ws.datas.Add(service, lism);
                            devices.Add(sk, ws);
                        }
                        else
                        {
                            if (devices[sk].datas.ContainsKey(service))
                            {
                                devices[sk].datas[service].Add(datas);
                            }
                            else
                            {
                                var lism = new List<string>();
                                lism.Add(datas);
                                devices[sk].datas.Add(service, lism);
                            }
                        }

                    }

                }

            } catch (Exception e) {
							Console.WriteLine(e.ToString());
						}
                return devices;
            }

            public void EmitMessage(List<JObject> m)
            {
                Console.WriteLine(">> Mensajes para emitir " +m.Count.ToString());
                var G = new List<JObject>();
                foreach (var message in m) {
                    G.Add(message);
                    if (G.Count == 200) {
                        this.GroupMessages(G);
                        G.Clear();
                    }

                }

                this.GroupMessages(G);   
            }

                private void GroupMessages(List<JObject> m)
			{

                Console.WriteLine(">> Emitiendo " + m.Count.ToString());

                if (this.socket != null){
				if (!string.IsNullOrEmpty (this.token)) {

					if (m.Count > 0) {

						foreach (var item in this.GroupSEmit(m)) {
							try {
								var wss = item.Value;

								StringBuilder sb = new StringBuilder ();
								StringWriter sw = new StringWriter (sb);

								using (JsonWriter writer = new JsonTextWriter (sw)) {
									//writer.Formatting = Formatting.Indented;

									writer.WriteStartObject ();
									writer.WritePropertyName ("DeviceKey");
									writer.WriteValue (wss.DeviceKey);
									writer.WritePropertyName ("token");
									writer.WriteValue (wss.token);

									foreach (var serv in wss.datas) {

										//writer.WritePropertyName (serv.Key);
										writer.WritePropertyName ("Request");
										writer.WriteStartArray ();	
											writer.WriteStartObject();
											writer.WritePropertyName("Service");
											writer.WriteValue(serv.Key);
											writer.WritePropertyName("Datas");
											writer.WriteStartArray ();
										foreach (var data in serv.Value) {
//										
												writer.WriteRawValue (data.ToString ());	
										}
											writer.WriteEnd ();
										
											writer.WriteEndObject ();
										writer.WriteEnd ();
									}


									writer.WriteEndObject ();
								}

								var message = sb.ToString ();
									//Console.WriteLine("Enviando...");
								this.socket.Emit ("wsservice", message);
							} catch (Exception ex) {
								Console.WriteLine (ex.ToString());
							}
						}

					} else {
						Console.WriteLine ("La lista de mensajes esta vacia...");
					}

				} else {
					Console.WriteLine ("No esta logeado... No se puede emitir...");
				}

			}else{
					Console.WriteLine ("Socket no se ha inicializado...");
			}
			}

			private string setIdQueueToData(string idQueue, string data){

				var d = DeserializeObject<Dictionary<string, object>> (data);
				if (d.ContainsKey ("idQueue")) {
					d["idQueue"] = idQueue;
				} else {
					d.Add ("idQueue", idQueue);
				}


				return SerializeObject<Dictionary<string, object>>(d);
			}

		

//			public T getRemoteObject<T>(string server, bool auto_save = true) where T : new(){
//
//				var R = new T();
//				if (String.IsNullOrEmpty (server) || String.IsNullOrEmpty (this.ObjectName)) {
//					throw new Exception("No ObjectName or server!");
//				}
//
//				server = this.CParam.Server + "/service/objects/view_equipment_service/r/" + ((int)DateTime.Now.Ticks).ToString () + "/" + ((int)DateTime.Now.Ticks).ToString () + "/"+ ((int)DateTime.Now.Ticks).ToString () + "/" + this.ObjectName;
//
//					string r = Post(server, new NameValueCollection()
//						{
//						{ "DeviceKey", this.CParam.DeviceKey },
//						{ "validator", this.CParam.DeviceKey}
//						});
//
//					string pattern = @"\[\{\""object\"":(.*?)\}\]";
//
//					Regex rex = new Regex(pattern);
//					MatchCollection matches = rex.Matches(r);
//
//					if (matches.Count > 0) {
//						foreach (Match match in matches) {
//							R = DeserializeObject<T> (match.Groups [1].Value);
//						break;
//						}
//					}
//
//				if(auto_save){
//					saveAnyLocalConfigObject (R, this.ObjectName, true);
//				}
//					
//				return R;
//			}
	
				

		}




	}



}

