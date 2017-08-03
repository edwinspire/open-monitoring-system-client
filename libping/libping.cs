//
//  Program.cs
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
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;


namespace OpenMonitoringSystem
{

	namespace Client
	{

	public struct PingParam{
		public int roundtriptime_threshold;
		public int roundtriptime_threshold_ideventtype;
		public int roundtriptime_ideventtype;
		public bool ReportOnlyOverThreshold;
		public string TargetIP;
		public int interval;
	};

	public struct PingReturn{public long roundtriptime; public string status; public string from; public string to;};

		public class Ping:OpenMonitoringSystem.Client.BaseBot{
		public PingParam Param = new PingParam();
		//public override string ObjectName { get { return "Ping"; } }
			public override string Service { get; set; }

		public override List<QueueItem> Run(){

			this.getLocalParams();
			var R = new EventData();
			var qi = new QueueItem ();
			var RL = new List<QueueItem>();
			var D = new PingReturn ();

			R.dateevent = new DateTime ();
			R.description = D.from;
			R.source = "00000000000000000000000000000000";
			D.to = Param.TargetIP;
			D.from = GetLocalIPAddress ();

			PingOptions options = new PingOptions ();

			// Use the default Ttl value which is 128, 
			// but change the fragmentation behavior.
			options.DontFragment = false;
			//long RoundtripTime = -1; 
			var pingSender = new System.Net.NetworkInformation.Ping ();

			try{

				if (string.IsNullOrEmpty(Param.TargetIP)) {
					Console.WriteLine("No ha ingresado una IP");
					throw new System.ArgumentException("Cannot be null or empty", "IP");
				} else {

					PingReply reply = pingSender.Send (Param.TargetIP, 5000);

					switch(reply.Status){
					case IPStatus.TtlExpired:

						break;
					default:

						break;
					}

					D.status = reply.Status.ToString ();

					D.roundtriptime = reply.RoundtripTime;

				//	Console.WriteLine ("Rep: {0}", reply.ToString ());
				//	Console.WriteLine ("Ttl: {0}", reply.Options.Ttl.ToString());

					if(D.roundtriptime > Param.roundtriptime_threshold){
						// Si el ping es alto, se espera 2 segundos y se vuelve a intentar
						System.Threading.Thread.Sleep(2000);
						pingSender = new System.Net.NetworkInformation.Ping ();
						reply = pingSender.Send (Param.TargetIP, 5000);
					//	Console.WriteLine ("Status: {0}", reply.Status.ToString ());
						D.status = reply.Status.ToString ();

						D.roundtriptime = reply.RoundtripTime;

					//	Console.WriteLine ("Rep: {0}", reply.ToString ());
					//	Console.WriteLine ("Ttl: {0}", reply.Options.Ttl.ToString());
					}

				}


				if(D.roundtriptime > Param.roundtriptime_threshold){
					R.ideventtype = Param.roundtriptime_threshold_ideventtype;
				}else{
					R.ideventtype = Param.roundtriptime_ideventtype;
				}

				R.details_json = D;
				R.dateevent = DateTime.Now;

			}catch(System.ArgumentException E){
				R.details_json = E;
				Console.WriteLine (E.ToString());
			}catch(System.Net.NetworkInformation.PingException E){
				R.details_json = E;
				Console.WriteLine (E.ToString());
			}
			catch(System.NullReferenceException E){
				R.details_json = E;
				Console.WriteLine (E.ToString());
			}
			catch(System.Exception E){
				R.details_json = E;
				Console.WriteLine (E.ToString());
			}

			qi.Datas = R;
 			RL.Add (qi);
			return RL;
		}

		public override void getLocalParams(){
			//this.Param = getAnyLocalObject <PingParam>("PingParam", config_path());
			this.interval = this.Param.interval;
		}



	}

	}


}
