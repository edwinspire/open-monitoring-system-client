//
//  Queue.cs
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
using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Linq;
using System.Web.Security;
using System.Net.Sockets;
using System.Dynamic;

namespace OpenMonitoringSystem
{

	namespace Client
	{

	public class QueueItem {
		public string idQueue;
		public string QueueService = "";
		//public DateTime DateQueue = DateTime.Now
		public string DeviceKey = "";
		public object Datas;
		public void genIDQueue(){
			var tempThis = this;
			tempThis.idQueue = "";
			var json = JsonConvert.SerializeObject (tempThis);
			this.idQueue = FormsAuthentication.HashPasswordForStoringInConfigFile(json, "MD5");
		}
	}


	public struct QueueParam{
		public string FileName;
		public QueueParam(string fn){
			if(String.IsNullOrEmpty(fn)){
				FileName = "Queue.s3";
			}else{
				FileName = fn;
			}
		}
	}

	public class ShippingQueue:BaseObject
	{
		public override string ObjectName { get { return "Queue"; } }
		public QueueParam Param = new QueueParam();

		public ShippingQueue ()
		{
		}

		public void Remove(string service, string DeviceKey, string idQueue){
			var f = Path.Combine(this.queue_dir(), DeviceKey, service, idQueue+".json");
			File.Delete(f);
		}

		public string queue_dir(){
			return Path.Combine(current_path(), "queue");
		}

		public List<JObject> getQueue(int max_doc = 1000){
			
			var R = new List<JObject> ();

			if(!Directory.Exists(this.queue_dir())){
				Directory.CreateDirectory (this.queue_dir());
			}
				
			foreach(var dir in Directory.GetDirectories(this.queue_dir())){
					
				DirectoryInfo d = new DirectoryInfo(dir);//Assuming Test is your Folder

				foreach(var device in d.GetDirectories()){
					var fs = device.GetFileSystemInfos("*.json"); //Getting Text files
					var orderedFiles = fs.OrderBy(f => f.CreationTime);

					foreach(FileInfo file in orderedFiles )
					{
						R.Add (getAnyLocalObject<JObject>(file.FullName));
						if(R.Count >= max_doc){
							break;
						}
					}	
				}
					
			}
			Console.WriteLine ("Documentos para enviar "+R.Count.ToString());
			return R;
		}

		public void Add(string QueueService, string DeviceKey, object Datas){
			var q = new QueueItem();
			q.QueueService = QueueService;
			q.DeviceKey = DeviceKey;
			q.Datas = Datas;
			this.Add(q);
		}

		public void Add(QueueItem item){

			if (string.IsNullOrEmpty(item.QueueService))
			{
				throw new System.ArgumentException("Cannot be null or empty", "QueueService");
			}

			if (string.IsNullOrEmpty(item.DeviceKey))
			{
				// Trata de obtener el DeviceKey del archivo de configuracion
					var CnxPara = getLocalObject<ConnectionParameters>();
						item.DeviceKey = CnxPara.DeviceKey;					
			}

			if (string.IsNullOrEmpty(item.DeviceKey))
			{
				throw new System.ArgumentException("Cannot be null or empty", "DeviceKey");
			}

			var id = "";
			//var services = "unknown";
			if (string.IsNullOrEmpty (item.idQueue)) {
				item.genIDQueue();
			} else {
				item.idQueue = id;
			}
			//if(!string.IsNullOrEmpty(item.QueueService)){
			//	services = item.QueueService;
			//}
			saveAnyLocalObject<QueueItem> (item, item.idQueue, Path.Combine(current_path(), "queue", item.DeviceKey, item.QueueService));
		}

		public static double DateTimeToUnixTimestamp(DateTime dateTime)
		{
			return (TimeZoneInfo.ConvertTimeToUtc(dateTime) - 
				new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc)).TotalSeconds;
		}
		public override void getLocalParams(){
			this.Param = this.getLocalObject <QueueParam>();
			//this.interval = this.Param.interval;
		}

	
	}



}

}