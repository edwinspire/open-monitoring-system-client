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
using System.Dynamic;

namespace OpenMonitoringSystem
{



	namespace Client{

		public struct ConnectionParameters{
			public string Server;
			public string ServerBackup;
			public string DeviceKey;
			public bool secure;
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

		/*
		public struct WSEvent{
			public string token;
			public int idequipment;
			public string validator;
			public System.Collections.Generic.List<EventData> events;
		}
		*/

		public struct WSSReturn{
			public string service;
			public string DeviceKey;
			public System.Collections.Generic.List<string> Return;
		}

		public struct WSService{
			public string token;
			//public string Service;
			//public DateTime DateQueue = DateTime.Now;
			public string DeviceKey;
			public System.Collections.Generic.Dictionary<string, List<string>> datas;
		}

		public class Comunicator:BaseObject{

			public ConnectionParameters CParam = new ConnectionParameters();
			public override string ObjectName { get { return "Comunicator"; } }
			public Quobject.SocketIoClientDotNet.Client.Socket socket;
			private string token = "qwerty1234";
			public event EventHandler OnReady;
			public ShippingQueue Queue = new ShippingQueue();

			public Comunicator(){

				this.CParam.Server = "http://localhost";
				//this.Param.idequipment = 0;
				this.CParam.DeviceKey = "abcdefghijklmnopqrstuvwzyz";
				//this.Param.service = "events.receiver";
				this.CParam = getLocalObject <ConnectionParameters>(); //this.Param.getLocalObject <ComunicatorParam>();
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


			private void QueueRemove(WSSReturn Return){

				Console.WriteLine (Return.Return.Count.ToString()+" mensajes para eliminar de la cola.");
				foreach(var q in Return.Return){
					this.Queue.Remove(Return.service, Return.DeviceKey, q);
				}

				Console.WriteLine ("Ok");
			}

			private List<EventData> QueueEventsRead(){

				var R = new List<EventData>();



				/*
				var connStr = "Data Source="+Path.Combine (current_path (), "config", "comunicator.s3")+"; Version=3;";

				using(var conn = new SQLiteConnection(connStr))
				{
					using(var cmd = new SQLiteCommand("SELECT datas FROM eventdata WHERE sent = 0;", conn)){

						conn.Open();

						using (var rdr = cmd.ExecuteReader())
						{
							while (rdr.Read()) 
							{
								var edata = DeserializeObject<EventData>(rdr.GetValue(0).ToString());
								R.Add (edata);
							}         
						}

						conn.Close();
					}
				}

				Console.WriteLine ("> Se enviarán "+R.Count.ToString()+" eventos.");
				*/
				return R;
			}

			public void SendByWS(){
				this.EmitMessage (this.Queue.getQueue ());
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
			private void connect(List<Agent> agents){

				this.getLocalParams ();
				var host = "ws://" + this.CParam.Server;
				var options = new IO.Options() { IgnoreServerCertificateValidation = true, AutoConnect = true, ForceNew = true };
				//this.socket = IO.Socket("http://www.farmaenlace.com:8093");
				if(this.CParam.secure == true){
					host = "wss://" + this.CParam.Server;
				}
				this.socket = IO.Socket(host, options);

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
			//				Console.WriteLine("< Se han guardado "+R.Count.ToString()+" mensajes.");
							QueueRemove(R);
						}

						//socket.Disconnect();
					});

				socket.On("connection", (e) =>
					{
						Console.WriteLine(this.CParam.Server);
						Console.WriteLine(e);
						this.token = "";
						socket.Emit("clogin", SerializeObject<ConnectionParameters>(this.CParam));
					});

				socket.On("clogged", (e) =>
					{
						this.token = e.ToString();
						Console.WriteLine("Se ha logueado...");
						foreach(var a in agents){
							a.StartInterval ();
						}
					});

				socket.On("token_expired", (e) =>
					{
						this.token = "";
						socket.Emit("clogin", SerializeObject<ConnectionParameters>(this.CParam));
					});


				socket.On(Quobject.SocketIoClientDotNet.Client.Socket.EVENT_RECONNECT, () =>
					{
						Console.WriteLine("EVENT_RECONNECT");
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

			public void  Connect(List<Agent> agents){
				Thread t = new Thread(()=>connect(agents));
				t.Start();
			}
				
			public void SetFromArgs(string[] args){
				if(args.Length > 1 && !String.IsNullOrEmpty(Convert.ToString(args[1]))){
					this.CParam.Server = args [1];
				}

				/*
				if(args.Length > 2 && !String.IsNullOrEmpty(Convert.ToString(args[2]))){
					this.CParam.idequipment = int.Parse( args [2]);
				}
				if(args.Length > 4 && !String.IsNullOrEmpty(Convert.ToString(args[4]))){
					this.CParam.service = args [4];
				}
				*/

				if(args.Length > 3 && !String.IsNullOrEmpty(Convert.ToString(args[3]))){
					this.CParam.DeviceKey = args [3];
				}


			}

			public Comunicator(string server, int idequipment, string validator){

				this.CParam.Server = server;
				//this.CParam.idequipment = idequipment;
				this.CParam.DeviceKey = validator;

			}

			public override void getLocalParams(){
//				this.Param = this.getLocalObject <ComunicatorParam>();
				this.CParam = getLocalObject <ConnectionParameters>(); //this.Param.getLocalObject <ComunicatorParam>();
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

			public static string Post(string uri, NameValueCollection pairs)
			{
				string response = "";
				try{
					using (WebClient client = new WebClient())
					{
						response = System.Text.Encoding.UTF8.GetString(client.UploadValues(uri, pairs));
					}
				}
				catch(Exception e){
					Console.WriteLine (e.Message);
					return null;
				}

				return response;
			}

			public List<string> SendMessage(System.Collections.Generic.List<JObject> ev)
			{
				var R = new List<string>();
				var datas = JsonConvert.SerializeObject (ev);
				var server = "http://"+this.CParam.Server + "/service/events/receiver/w/" + Md5(datas) + "/" + ((int)DateTime.Now.Ticks).ToString () + "/"+ ((int)DateTime.Now.Ticks).ToString () + "/"+ev.Count.ToString();

				if(ev.Count > 0){
					var	txt = Post(server, new NameValueCollection()
						{
							{ "DeviceKey", this.CParam.DeviceKey},
							{ "list_events", datas}
						});

					R = getEventsReceived (txt);

				}
				return R;
			}

			private List<string> getEventsReceived(string response){
				string pattern = @"\[\{\""fun_receiver_json\"":(.*?)\}\]";
				var R = new List<string> ();
				Regex rex = new Regex(pattern);
				var txt = response.Replace ("\n", "").Replace (" ", "");
				MatchCollection matches = rex.Matches(txt);

				if (matches.Count > 0) {
					foreach (Match match in matches) {
						R = DeserializeObject<List<string>> (match.Groups [1].Value);
					}
				}
				return R;
			}

			private List<List<JObject>> MessageGroup(List<JObject> m){
				return m
					.GroupBy (u => u.GetValue ("DeviceKey"))
					//.OrderBy (u => "DateQueue")
					.Select(grp => grp.ToList())
					.ToList();
			}

			public void EmitMessage(List<JObject> m)
			{

				if(this.socket != null){
				if (!string.IsNullOrEmpty (this.token)) {

					if (m.Count > 0) {

						var devices = new System.Collections.Generic.Dictionary<string, WSService> ();

						try {

							foreach (var a in MessageGroup(m)) {

								foreach (var b in a) {
									var service = b.GetValue ("QueueService").ToString ();
									var key = b.GetValue ("DeviceKey").ToString ();
										var idQueue = b.GetValue ("idQueue").ToString();
										var datas = setIdQueueToData(idQueue, b.GetValue ("Datas").ToString());

									var sk = key + "-" + service;

									if (!devices.ContainsKey (sk)) {
										var ws = new WSService ();
										ws.datas = new Dictionary<string, List<string>> ();
										ws.DeviceKey = key;
										ws.token = this.token;
										var lism = new List<string> ();
										
											lism.Add (datas);
										ws.datas.Add (service, lism);
										devices.Add (sk, ws);
									} else {
										if (devices [sk].datas.ContainsKey (service)) {
											devices [sk].datas [service].Add (datas);
										} else {
											var lism = new List<string> ();
											lism.Add (datas);
											devices [sk].datas.Add (service, lism);
										}
									}

								}

							}

						} catch (Exception e) {
							Console.WriteLine (e.Message);
						}


						foreach (var item in devices) {
							try {
								var wss = item.Value;

								StringBuilder sb = new StringBuilder ();
								StringWriter sw = new StringWriter (sb);

								using (JsonWriter writer = new JsonTextWriter (sw)) {
									writer.Formatting = Formatting.Indented;

									writer.WriteStartObject ();
									writer.WritePropertyName ("DeviceKey");
									writer.WriteValue (wss.DeviceKey);
									writer.WritePropertyName ("token");
									writer.WriteValue (wss.token);

									foreach (var serv in wss.datas) {

										writer.WritePropertyName (serv.Key);
										writer.WriteStartArray ();

										foreach (var data in serv.Value) {
											writer.WriteRawValue (data.ToString ());	
										}
										//writer.WriteRawValue(sb1.ToString());
										writer.WriteEnd ();
									}


									writer.WriteEndObject ();
								}

								var message = sb.ToString ();

								this.socket.Emit ("wsservice", message);
							} catch (Exception ex) {
								Console.WriteLine (ex.Message);
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

			public void EmitEvents(System.Collections.Generic.List<EventData> ev)
			{
				/*
				if(!string.IsNullOrEmpty(this.token)){

					if(ev.Count > 0){
						var E = new WSEvent ();
						E.token = this.token;
						E.idequipment = this.Param.idequipment;
						E.validator = this.Param.validator;
						E.events = ev;
						try{
							this.socket.Emit("cevents", SerializeObject<WSEvent>(E));
						}catch(Exception ex){
							Console.WriteLine (ex.Message);
						}
					}else{
						Console.WriteLine ("La lista de eventos esta vacia...");
					}

				}else{
					Console.WriteLine ("No esta logeado... No se puede emitir...");
				}
				*/
			}

			public T getRemoteObject<T>(string server, bool auto_save = true) where T : new(){

				var R = new T();
				if (String.IsNullOrEmpty (server) || String.IsNullOrEmpty (this.ObjectName)) {
					throw new Exception("No ObjectName or server!");
				}

				server = this.CParam.Server + "/service/objects/view_equipment_service/r/" + ((int)DateTime.Now.Ticks).ToString () + "/" + ((int)DateTime.Now.Ticks).ToString () + "/"+ ((int)DateTime.Now.Ticks).ToString () + "/" + this.ObjectName;

					string r = Post(server, new NameValueCollection()
						{
						{ "DeviceKey", this.CParam.DeviceKey },
						{ "validator", this.CParam.DeviceKey}
						});

					string pattern = @"\[\{\""object\"":(.*?)\}\]";

					Regex rex = new Regex(pattern);
					MatchCollection matches = rex.Matches(r);

					if (matches.Count > 0) {
						foreach (Match match in matches) {
							R = DeserializeObject<T> (match.Groups [1].Value);
						break;
						}
					}

				if(auto_save){
					this.saveObjectConfig (R, this.ObjectName, true);
				}
					
				return R;
			}
	
				

		}




	}



}

