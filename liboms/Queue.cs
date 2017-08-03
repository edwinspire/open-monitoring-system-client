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

namespace OpenMonitoringSystem
{

	namespace Client
	{

		public class QueueItem :ICloneable{
		public string idQueue;
		public string QueueService = "";
		public DateTime DateQueue = DateTime.Now;
		public string DeviceKey = "";
		public object Datas;
		public void genIDQueue(){
				QueueItem tempThis = (QueueItem)this.Clone();
			tempThis.idQueue = "";
				tempThis.DateQueue = new DateTime(1900, 1, 1);
			var json = JsonConvert.SerializeObject (tempThis);
			this.idQueue = FormsAuthentication.HashPasswordForStoringInConfigFile(json, "MD5");
		}
			public object Clone()
			{
				return this.MemberwiseClone();
			}
	}


	public struct QueueParam{
			public int MaxMessagesToSend;
			public int TimeOutResend;
	}

		public enum QueueMessageStatus{
			NotSent = 0,
			Sending = 1,
			Sent = 2
		}

	public class ShippingQueue:BaseObject
	{
			//public override string ObjectName { get { return "ShippingQueuexxx"; } }
		public QueueParam Param = new QueueParam();
			//public int MaxToSend = 1000;

		public ShippingQueue ()
		{
				getLocalParams ();				
		}

//			private void CheckPath(){
//				if(string.IsNullOrEmpty(this.BaseConfig.QueuePath)){
//					this.BaseConfig.QueuePath = this.queue_dir ();
//				}
//
//				if(!Directory.Exists(this.BaseConfig.QueuePath)){
//					Directory.CreateDirectory (this.BaseConfig.QueuePath);
//				}
//			}

			public void DeleteSentMessages(){
				var R = new Dictionary<string, JObject> ();

			//	CheckPath ();

				foreach(var dir in Directory.GetDirectories(this.BaseConfig.QueuePath)){

					DirectoryInfo d = new DirectoryInfo(dir);//Assuming Test is your Folder

					foreach(var device in d.GetDirectories()){
	
						var fs = d.GetFileSystemInfos(QueueMessageStatus.Sent.ToString()+".*.json"); //Getting Text files
						var orderedFiles = fs.OrderBy(f => f.CreationTime);
						foreach (FileInfo file in orderedFiles) {
							file.Delete ();
						}

					}

				}
			}

			public void ResetMessageToSend(){
				var R = new Dictionary<string, JObject> ();
			
			//	CheckPath ();

				foreach(var dir in Directory.GetDirectories(this.BaseConfig.QueuePath)){

					DirectoryInfo d = new DirectoryInfo(dir);//Assuming Test is your Folder

					foreach(var device in d.GetDirectories()){

						var filter = QueueMessageStatus.Sending.ToString () + ".*.json";
						var fs = device.GetFileSystemInfos(filter); //Getting Text files

						var orderedFiles = fs.Where(f => f.CreationTime.AddSeconds(this.Param.TimeOutResend) < DateTime.Now).OrderBy (f => f.CreationTime);
						foreach (FileInfo file in orderedFiles) {
							file.MoveTo(file.FullName.Replace(QueueMessageStatus.Sending.ToString()+".", QueueMessageStatus.NotSent.ToString()+"."));
						}

					}

				}
			}


			public void ChangeStatusMessage(string service, string DeviceKey, string idQueue, QueueMessageStatus current_status, QueueMessageStatus new_status){
				var prefix = "";
				var path = "";

			//	CheckPath ();

				prefix = current_status.ToString () + ".";

				path = Path.Combine(this.BaseConfig.QueuePath, DeviceKey, service, prefix+idQueue+".json");

				if(File.Exists(path)){
					File.Move(path, Path.Combine(this.BaseConfig.QueuePath, DeviceKey, service, new_status.ToString()+"."+idQueue+".json"));	
				}else{
				//	Console.WriteLine ("El archivo "+path+" no existe y no se puede cambiar de estado de "+current_status.ToString()+" a "+new_status.ToString());
				}
					
		}
				

			public List<JObject> ToSend(int max_doc = 1000){
				var R = new List<JObject> ();

				this.ResetMessageToSend ();

				foreach (var item in this.getQueue(QueueMessageStatus.NotSent, max_doc)) {

					try {
						ChangeStatusMessage (item.GetValue("QueueService").ToString(), item.GetValue("DeviceKey").ToString(), item.GetValue("idQueue").ToString(), QueueMessageStatus.NotSent, QueueMessageStatus.Sending);
						R.Add (item);
					} catch (Exception e) {
						Console.WriteLine (e.Message);
					}

				}
				return R;
			}

			private List<JObject> getQueue(QueueMessageStatus status, int max_doc = 1000){
			
				var R = new List<JObject> ();
			
				//this.CheckPath ();

				foreach(var dir in Directory.GetDirectories(this.BaseConfig.QueuePath)){
					
				DirectoryInfo d = new DirectoryInfo(dir);//Assuming Test is your Folder

				foreach(var device in d.GetDirectories()){
						R.AddRange (this.GetIndividual (device, status));
//						foreach(var item in this.GetIndividual (device)){
//							if(!R.ContainsKey(item.Key)){
//								R.Add (item.Key, item.Value);
//							}	
//						}
							
				}
					
			}
			Console.WriteLine ("Documentos para enviar "+R.Count.ToString());
			return R;
		}

			private List<JObject> getJObject(IOrderedEnumerable<FileSystemInfo> fsi){
				var R = new  List<JObject> ();
				foreach(FileInfo file in fsi )
				{
					R.Add (getAnyLocalObject<JObject>(file.FullName));
//					foreach(var item in this.GetFile(file.FullName)){
//						R.Add (item.Key ,item.Value);
//					}

					//R.AddRange (this.GetFile(file.FullName));
//					if(R.Count >= this.MaxToSend){
//						break;
//					}
				}
				return R;
			}

			public List<JObject> GetIndividual(DirectoryInfo dir, QueueMessageStatus status){

				var fs = dir.GetFileSystemInfos(GetFilterByStatus(status)); //Getting Text files
				var orderedFiles = fs.OrderBy(f => f.CreationTime);

				return getJObject(orderedFiles);
			}

			public List<JObject> GetIndividual(QueueMessageStatus status){
				var R = new List<JObject> ();
				DirectoryInfo d = new DirectoryInfo(this.BaseConfig.QueuePath);//Assuming Test is your Folder

				var fs = d.GetFileSystemInfos(GetFilterByStatus(status)); //Getting Text files
				var orderedFiles = fs.OrderBy(f => f.CreationTime);

				return getJObject(orderedFiles);
			}

			private string GetFilterByStatus(QueueMessageStatus status){
//				var _status = "*.json";
//				if(status != QueueMessageStatus.NotSent){
//					_status = status.ToString () + ".*.json";
//				}
				return status.ToString () + ".*.json";;
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
					var CnxPara = getConfigObject<Device>("Device");
					item.DeviceKey = CnxPara.Key;					
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
				var path = Path.Combine (this.BaseConfig.QueuePath, item.DeviceKey, item.QueueService);

				var fnameSent = QueueMessageStatus.Sent + "." + item.idQueue+".json";
				var fnameNSending = QueueMessageStatus.Sending + "." + item.idQueue+".json";
			//if(!string.IsNullOrEmpty(item.QueueService)){
			//	services = item.QueueService;
			//}

				if(!File.Exists(Path.Combine(path, fnameSent)) || !File.Exists(Path.Combine(path, fnameNSending))     ){

					CreateDir (path);

					var fnameNSent = Path.Combine(path, QueueMessageStatus.NotSent + "." + item.idQueue+".json");
					saveAnyLocalObject<QueueItem> (item, fnameNSent, false);
				}

		}

		public static double DateTimeToUnixTimestamp(DateTime dateTime)
		{
			return (TimeZoneInfo.ConvertTimeToUtc(dateTime) - 
				new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc)).TotalSeconds;
		}
		public override void getLocalParams(){
				var Save = false;
				this.Param = getConfigObject <QueueParam>("QueueParam");
			//	CheckPath ();

				if(this.Param.TimeOutResend < 5){
					this.Param.TimeOutResend = 30;
					Save = true;
				}

				if(this.Param.MaxMessagesToSend < 1){
					this.Param.MaxMessagesToSend = 1;	
					Save = true;
				}

				if(Save){
					saveConfigObject <QueueParam>(this.Param, "QueueParam", this.BaseConfig.ConfigPath);	
				}

			//this.interval = this.Param.interval;
		}

	
	}



}

}