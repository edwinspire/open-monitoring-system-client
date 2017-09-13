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

namespace OpenMonitoringSystem
{


	public class EventData {
		public DateTime dateevent;
		public int ideventtype;
		public string description;
		public string source;
		public object details_json;
	}




	public class OMSEventsList : EventArgs
	{
		public List<EventData> events { get; private set; }

		public OMSEventsList(List<EventData> e)
		{
			events = e;
		}
	}


	namespace Client{



		public class Bot:BaseBot{
			public override string Service { get; set;}
			public override List<QueueItem> Run(){
				throw new System.ArgumentException("Method no valid", "Run");
			}
			public override void getLocalParams(){

			}

		}




		public abstract class BaseBot:BaseObject{

			public delegate void events(object sender, OMSEventsList e);
			//public event events OnEvents;
			private bool stopInterval = false;
			private bool runnigInterval = false;
			public Device CDevice = new Device();
			public int interval = 10;
			public ShippingQueue Queue = new ShippingQueue();
			public abstract string Service { get; set;}

			public override abstract void getLocalParams();
			public abstract List<QueueItem> Run();

			public BaseBot(){
				getParams();
			}

			public void RunThread(){
				Thread t = new Thread(()=>_Run());
				t.Start();
			}

			private void getParams(){
				//this.Queue.Param = getAnyLocalObject <QueueParam>("QueueParam", this.BaseConfig.ConfigPath);
				this.CDevice = getConfigObject <Device>("Device"); 

				if (string.IsNullOrEmpty(this.CDevice.Key))
				{
					throw new System.ArgumentException("Cannot be null or empty. Config Path: "+this.BaseConfig.ConfigPath, "Key");
				}

			
			}

			private void  _Run(){
				foreach(var a in this.Run () ){
					var x = a;
//					if(string.IsNullOrEmpty(this.Service) || string.IsNullOrEmpty(this.CDeviceDeviceKey)){
//						this.CParam = getAnyLocalConfigObject<ConnectionParameters> ("ConnectionParameters");
//					}
						
					x.QueueService = this.Service;
					x.DeviceKey = this.CDevice.Key;
					this.Queue.Add (x);	
				}	
			}

			public void Message(QueueItem item){
				this.Queue.Add(item);
			}

			public void Message(string QueueService, string DeviceKey, object Datas){
				var q = new QueueItem();
				q.QueueService = QueueService;
				q.DeviceKey = DeviceKey;
				q.Datas = Datas;
				this.Queue.Add(q);
			}

			public void Message(object Datas){
				var q = new QueueItem();
				q.QueueService = this.Service;
				q.DeviceKey = this.CDevice.Key;
				q.Datas = Datas;
				this.Queue.Add(q);
			}

			public static void AddMessage(QueueItem item){
				var Queue = new ShippingQueue ();
				Queue.Add (item);
			}

			public static void AddMessage(string QueueService, string DeviceKey, object Datas){

				var Queue = new ShippingQueue ();
				var q = new QueueItem();
				q.QueueService = QueueService;
				q.DeviceKey = DeviceKey;
				q.Datas = Datas;
				Queue.Add(q);
			}



			private void RunThreadIntervals(){
				if(!runnigInterval){
					this.stopInterval = false;
					while(!this.stopInterval){
						this._Run ();
						//this.EmitEvents(this.Run());
						if(this.interval < 10){
							this.interval = 10;
						}
						Thread.Sleep (this.interval * 1000);
					}
					runnigInterval = false;
				}
			}

			public void StopInterval(){
				this.stopInterval = true;
			}

			public void StartInterval(){
				Thread t = new Thread(()=>RunThreadIntervals());
				t.Start();					
			}

			public string genIDQueue(QueueItem ev){
				ev.idQueue = "";
				return Md5(SerializeObject<QueueItem> (ev));
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
					Console.WriteLine (e.ToString());
				}

				return response;
			}


				

				

		}

	


	}



}

