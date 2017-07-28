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

	public abstract class BaseObject{

		public abstract string ObjectName { get;}
		public abstract void getLocalParams();

		public BaseObject(){

		}

		public static T getAnyLocalObject<T>(string full_path)  where T : new(){

			if (!File.Exists(full_path))
			{
				saveAnyLocalObject<T> (new T(), full_path, true);
			}
				
			string jsonobj = File.ReadAllText(full_path);
			return DeserializeObject<T>(jsonobj);
		}

		public static T getAnyLocalObject<T>(string name, string path, bool replace = true)  where T : new(){

			if (string.IsNullOrEmpty (name)) {
				throw new System.ArgumentException ("Cannot be null or empty", "name");
			}
			 
			if (string.IsNullOrEmpty (path)) {
				path = current_path ();
			}

			if (!Directory.Exists (Path.GetDirectoryName (path))) {
				System.IO.Directory.CreateDirectory (Path.GetDirectoryName (path));
			}
				
			var full_path = Path.Combine (path, name + ".json");

			return getAnyLocalObject <T>(full_path);
		}

		public T getLocalObject<T>() where T : new(){
			var name = this.ObjectName;
			if(string.IsNullOrEmpty(this.ObjectName)){
				name = this.GetType ().Name;
			}
			var full_path = Path.Combine(current_path(), "config");
			return getAnyLocalObject<T>(name, full_path, true);
		}

		public T getLocalObject<T>(string full_path) where T : new(){
			return getAnyLocalObject<T>(this.ObjectName, full_path, true);
		}

		public static string Md5(string text){
			return FormsAuthentication.HashPasswordForStoringInConfigFile(text, "MD5");
		}

		private void CreateConfigDir(string folder){
			System.IO.Directory.CreateDirectory (Path.Combine(current_path(), folder));
		}

		public void saveObjectConfig<T>(T obj, string name, bool replace = true){
			if(!string.IsNullOrEmpty(name)){
				name = obj.GetType ().Name;
			}
			saveAnyLocalObject<T>(obj, name, config_path(), replace);

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
				Console.WriteLine (e.Message);
			}
				
		}

		public static void saveAnyLocalObject<T>(T obj, string name, string path, bool replace = true){

			if(string.IsNullOrEmpty(name)){
				throw new System.ArgumentException("Cannot be null or empty", "name");
			}

			if(string.IsNullOrEmpty(path)){
				path = current_path();
			}

			if (!Directory.Exists(path)) {
				System.IO.Directory.CreateDirectory (path);
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
		public static string config_path(){
			return Path.Combine(current_path(), "config");
		}

	}


}

