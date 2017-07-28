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
using Quobject.SocketIoClientDotNet.Client;
using System.Threading;

namespace OpenMonitoringSystem
{




/// <summary>
/// ////////////////////////////////////////////
/// </summary>
	class OMSComunicator
	{
		public static void Main (string[] args)
		{
			Console.WriteLine ("Start Comunicator!", args.ToString());

			var List = new List<Client.Agent> ();
		/*	var a1 = new Ping ();
			var a2 = new Ping ();
			var a3 = new Ping ();
			var a4 = new Ping ();


			List.Add (a1);
			List.Add (a2);
			List.Add (a3);
			List.Add (a4);
			*/

			var Comunic = new Client.Comunicator ();

			Comunic.Connect (List);

			var maxMinutes = 60;

			while(maxMinutes > 0){
				Comunic.SendByWS ();
				Thread.Sleep (5000);	
			//	Console.WriteLine (maxMinutes.ToString());
				maxMinutes--;
			}

		//	Console.ReadLine();

		}




	}

}
