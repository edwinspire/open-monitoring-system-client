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

namespace OpenMonitoringSystem
{

	public struct ConfigParam{
		public string QueuePath;
		public string ConfigPath;
	}

	public abstract class BaseObject{

		//public abstract string ObjectName { get;}
		public abstract void getLocalParams();
		public ConfigParam BaseConfig = new ConfigParam();

		public BaseObject(){
			ReadConfig();
		}

		public string queue_dir(){
			return Path.Combine(current_path(), "queue");
		}

		private void ReadConfig(){
			var SaveConfig = false;
			this.BaseConfig = getAnyLocalConfigObject<ConfigParam>("BaseConfig");
			if(string.IsNullOrEmpty(this.BaseConfig.ConfigPath)){
				this.BaseConfig.ConfigPath = config_dir ();
				SaveConfig = true;
			}
			if(string.IsNullOrEmpty(this.BaseConfig.QueuePath)){
				this.BaseConfig.QueuePath = queue_dir ();
				SaveConfig = true;
			}

			if(SaveConfig){
				saveObjectConfig<ConfigParam> (this.BaseConfig, "BaseConfig");
			}

			CreateDir (this.BaseConfig.QueuePath);
			CreateDir (this.BaseConfig.ConfigPath);
 			getLocalParams ();
		}

		public void CreateDir(string path){
			if (!Directory.Exists  (path)) {
				System.IO.Directory.CreateDirectory (path);
			}
		}

		public static T getAnyLocalObject<T>(string full_path)  where T : new(){

			if (!File.Exists(full_path))
			{
				saveAnyLocalObject<T> (new T(), full_path, true);
            }
            try {

                using (var fileStream = new FileStream(full_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                    using (var textReader = new StreamReader(fileStream))
                    {
                        var jsonobj = textReader.ReadToEnd();
                        //string jsonobj = File.ReadAllText(full_path);
                        return DeserializeObject<T>(jsonobj);
                    }
                }
                
                
            } catch (Exception e) {
                return new T();
            }
			
		}

		public static T getAnyLocalConfigObject<T>(string name, bool replace = true)  where T : new(){

			return getAnyLocalObject<T>(name, config_dir(), replace);
		}

		public T getConfigObject<T>(string name, bool replace = true) where T : new(){
			return getAnyLocalObject<T>(name, this.BaseConfig.ConfigPath, replace);
		}

		public static T getAnyLocalObject<T>(string name, string path, bool replace = true)  where T : new(){

			if (string.IsNullOrEmpty (name)) {
				throw new System.ArgumentException ("Cannot be null or empty", "name");
			}
			 
			if (string.IsNullOrEmpty (path)) {
				path = current_path ();
			}

			if (!Directory.Exists  (path)) {
				System.IO.Directory.CreateDirectory (path);
			}
				
			var full_path = Path.Combine (path, name + ".json");

			return getAnyLocalObject <T>(full_path);
		}

//		public T getLocalObject<T>() where T : new(){
//			var name = this.ObjectName;
//			if(string.IsNullOrEmpty(name)){
//				name = this.GetType ().Name;
//			}
//			var full_path = Path.Combine(current_path(), "config");
//			return getAnyLocalObject<T>(name, full_path, true);
//		}

//		public T getLocalObject<T>(string full_path) where T : new(){
//			return getAnyLocalObject<T>(this.ObjectName, full_path, true);
//		}

		public static string Md5(string text){
			return FormsAuthentication.HashPasswordForStoringInConfigFile(text, "MD5");
		}

		private void CreateConfigDir(string folder){
			System.IO.Directory.CreateDirectory (Path.Combine(current_path(), folder));
		}

		public void saveObjectConfig<T>(T obj, string name, bool replace = true){
			if(string.IsNullOrEmpty(name)){
				name = obj.GetType ().Name;
			}
			saveAnyLocalObject<T>(obj, name, config_dir(), replace);

			/*
			var dir = Path.Combine(current_path(), subfolder);
			var f = "";
			if(fileName.Length > 0){
				f = fileName;
			}else{
				f = ObjectName;
			}

			var full_path = Path.Combine(dir, f+".json");

			try{
				// This text is added only once to the file.
				if (!Directory.Exists(dir)) {
					CreateConfigDir (subfolder);
				}

				var ser_obj = SerializeObject (obj);

				if (!replace) {
					File.WriteAllText (full_path, ser_obj);	
				} else if (File.Exists (full_path) && !replace) {
					Console.WriteLine ("El archivo existe, no se reemplazará.");
				} else {
					File.WriteAllText (full_path, ser_obj);	
				}
			}catch(Exception e){
				Console.WriteLine (e.Message);
			}
			*/
		}

		public static void saveAnyLocalObject<T>(T obj, string full_path, bool replace = true){

			if(string.IsNullOrEmpty(full_path)){
				throw new System.ArgumentException("Cannot be null or empty", "name");
			}

			try{
					
				var ser_obj = SerializeObject (obj);

				if (!replace) {
					File.WriteAllText (full_path, ser_obj);	
				} else if (File.Exists (full_path) && !replace) {
					Console.WriteLine ("El archivo existe, no se reemplazará.");
				} else {
					File.WriteAllText (full_path, ser_obj);	
				}
			}catch(Exception e){
				Console.WriteLine (e.ToString());
			}
				
		}

		public static void saveAnyLocalConfigObject<T>(T obj, string name, bool replace = true){
			 saveAnyLocalObject<T>(obj, name, config_dir(), replace);
		}

		public void saveConfigObject<T>(T obj, string name, string path, bool replace = true){
			 saveAnyLocalObject<T>(obj, name, this.BaseConfig.ConfigPath, replace);
		}

		public static void saveAnyLocalObject<T>(T obj, string name, string path, bool replace = true){

			if(string.IsNullOrEmpty(name)){
				throw new System.ArgumentException("Cannot be null or empty", "name");
			}

			if(string.IsNullOrEmpty(path)){
				path = current_path();
			}
				
			saveAnyLocalObject <T>(obj, Path.Combine(path, name+".json"));
		}

		public static string SerializeObject<T>(T obj){
			return JsonConvert.SerializeObject(obj);
		}
		public static T DeserializeObject<T>(string obj) where T : new(){
			return JsonConvert.DeserializeObject<T>(obj);
		}

		public static string current_path(){
			return System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
		}
		public static string config_dir(){
			return Path.Combine(current_path(), "config");
		}

	}


}

