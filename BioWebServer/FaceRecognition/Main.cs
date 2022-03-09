/**
	\file csharp_demo/video_recognition_demo/src/Main.cs
*/


using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;

using CommandLine;

using VDT.FaceRecognition.SDK;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Net;

public static class MyConstants
{
	public static string[] MyFirstConstant=new string[15];
	public static int cnt = 0;
	public static double[] livescore = new double[15];
	public static string[] matchedwith = new string[15];
}


class Options
{
	[Option("config_dir", Default = "../../../conf/facerec", HelpText = "Path to config directory.")]
	public string config_dir { get; set; }




	[Option("license_dir", Default = null, HelpText = "Path to license directory [optional].")]
	public string license_dir { get; set; }

	[Option("database_dir", Default = "../../base", HelpText = "Path to database directory.")]
	public string database_dir { get; set; }

	[Option("method_config", Default = "method6v7_recognizer.xml", HelpText = "Recognizer config file.")]
	public string method_config { get; set; }

	[Option("recognition_distance_threshold", Default = 7000.0f, HelpText = "Recognition distance threshold.")]
	public float recognition_distance_threshold { get; set; }

	[Option("frame_fps_limit", Default = 25f, HelpText = "Frame fps limit.")]
	public float frame_fps_limit { get; set; }

	[Value(0, MetaName = "video_sources", HelpText = "List of video sources (id of web-camera, url of rtsp stream or path to video file)")]
	public IEnumerable<string> video_sources { get; set; }
};

class VideoRecognitionDemo
{
	public static FacerecService service;
	public static Recognizer recognizer;
	public static Capturer capturer;
	public static Database database;
	public static VideoWorker video_worker;

	static int test(string[] args)
	{

		StartServer(args);


		string m = faceDec(args);
	
		string[] b = MyConstants.MyFirstConstant;
		int x = MyConstants.cnt;
		string k = "";
		return 0;
	}

	static string faceDec(string[] args)
	{
		try
		{
			
			// print usage
			Console.WriteLine("Usage: dotnet csharp_video_recognition_demo.dll [OPTIONS] <video_source>...");
			Console.WriteLine("Examples:");
			Console.WriteLine("    Webcam:  dotnet csharp_video_recognition_demo.dll --config_dir ../../../conf/facerec 0");
			Console.WriteLine("    RTSP stream:  dotnet csharp_video_recognition_demo.dll --config_dir ../../../conf/facerec rtsp://localhost:8554/");
			Console.WriteLine("");
			
			// parse arguments
			bool error = false;
			Options options = new Options();
			CommandLine.Parser.Default.ParseArguments<Options>(args)
				.WithParsed<Options>(opts => options = opts)
				.WithNotParsed<Options>(errs => error = true);

			// exit if argument parsign error
			if (error) return "" ;

			// print values of arguments
			Console.WriteLine("Arguments:");
			foreach (var opt in options.GetType().GetProperties())
			{
				if (opt.Name == "video_sources")
				{
					Console.Write("video sources = ");
					foreach (string vs in options.video_sources)
					{
						Console.Write(vs + " ");
					}
					Console.WriteLine();
				}
				else
				{
					Console.WriteLine("--{0} = {1}", opt.Name, opt.GetValue(options, null));
				}  
			}
			Console.WriteLine("\n");

			//parameters parse
			string config_dir = options.config_dir;
			string license_dir = options.license_dir;
			string database_dir = options.database_dir;
			string method_config = options.method_config;
			float recognition_distance_threshold = options.recognition_distance_threshold;
			float frame_fps_limit = options.frame_fps_limit;
			List<string> video_sources = new List<string>(options.video_sources);

			// check params
			MAssert.Check(config_dir != string.Empty, "Error! config_dir is empty.");
			MAssert.Check(database_dir != string.Empty, "Error! database_dir is empty.");
			MAssert.Check(method_config != string.Empty, "Error! method_config is empty.");
			MAssert.Check(recognition_distance_threshold > 0, "Error! Failed recognition distance threshold.");

			List<ImageAndDepthSource> sources = new List<ImageAndDepthSource>();
			List<string> sources_names = new List<string>();


			MAssert.Check(video_sources.Count > 0, "Error! video_sources is empty.");

			for (int i = 0; i < video_sources.Count; i++)
			{
				sources_names.Add(string.Format("OpenCvS source {0}", i));
				//sources.Add(new OpencvSource(video_sources[i]));
			}


			MAssert.Check(sources_names.Count == sources.Count);

			// print sources
			Console.WriteLine("\n{0} sources: ", sources.Count);

			for (int i = 0; i < sources_names.Count; ++i)
				Console.WriteLine("  {0}", sources_names[i]);
			Console.WriteLine("");

			// create facerec servcie
			if (service != null) { service = null; }
			/*FacerecService*/ service =
				FacerecService.createService(
					config_dir,
					license_dir);

			Console.WriteLine("Library version: {0}\n", service.getVersion());

			// create database
			if (recognizer!=null) { recognizer = null; }
			/*Recognizer*/ recognizer = service.createRecognizer(method_config, true, false, false);
			if (capturer != null) { capturer = null; }
			/*Capturer */
			capturer = service.createCapturer("common_capturer4_lbf_singleface.xml");
			/*Database*/ database = new Database(
				database_dir,
				recognizer,
				capturer,
				recognition_distance_threshold);
			recognizer.Dispose();
			capturer.Dispose();

			FacerecService.Config vw_config = new FacerecService.Config("video_worker_fdatracker_blf_fda.xml");
			// vw_config.overrideParameter("single_match_mode", 1);
			vw_config.overrideParameter("search_k", 10);
			vw_config.overrideParameter("not_found_match_found_callback", 1);
			vw_config.overrideParameter("downscale_rawsamples_to_preferred_size", 0);

			//ActiveLiveness.CheckType[] checks = new ActiveLiveness.CheckType[3]
			//{
			//	ActiveLiveness.CheckType.BLINK,
			//			ActiveLiveness.CheckType.TURN_RIGHT,
			//			ActiveLiveness.CheckType.SMILE
			//};


			// create one VideoWorker
			if (video_worker!=null) { video_worker = null; }
			/*VideoWorker*/ video_worker =
				service.createVideoWorker(
					new VideoWorker.Params()
						.video_worker_config(vw_config)
						.recognizer_ini_file(method_config)
						.streams_count(sources.Count)
						//.age_gender_estimation_threads_count(sources.Count)
						//.emotions_estimation_threads_count(sources.Count)
						//.active_liveness_checks_order(checks)
						.processing_threads_count(sources.Count)
						.matching_threads_count(sources.Count));

			// set database
			video_worker.setDatabase(database.vwElements, Recognizer.SearchAccelerationType.SEARCH_ACCELERATION_1);

			for (int i = 0; i < sources_names.Count; ++i)
			{
				OpenCvSharp.Window window = new OpenCvSharp.Window(sources_names[i]);

				OpenCvSharp.Cv2.ImShow(sources_names[i], new OpenCvSharp.Mat(100, 100, OpenCvSharp.MatType.CV_8UC3, OpenCvSharp.Scalar.All(0)));
			}

			// prepare buffers for store drawed results
			Mutex draw_images_mutex = new Mutex();
			List<OpenCvSharp.Mat> draw_images = new List<OpenCvSharp.Mat>(sources.Count);

			// create one worker per one source
			List<Worker> workers = new List<Worker>();
			FacerecService facerecService = FacerecService.createService(options.config_dir, options.license_dir);

			Liveness2DEstimator _liveness_2d_estimator = facerecService.createLiveness2DEstimator("liveness_2d_estimator_v2.xml");





			for (int i = 0; i < sources.Count; ++i)
			{
				string val = "";
				draw_images.Add(new OpenCvSharp.Mat(100, 100, OpenCvSharp.MatType.CV_8UC3, OpenCvSharp.Scalar.All(0)));
				//workers.Add(new Worker(
				//	database,
				//	video_worker,
				//	sources[i],
				//	i,  // stream_id
				//	draw_images_mutex,
				//	draw_images[i],
				//	frame_fps_limit, _liveness_2d_estimator,val,service,capturer
				//	));
				

			}

			// draw results until escape presssed
			for (; ; )
			{

				{
					draw_images_mutex.WaitOne();
					for (int i = 0; i < draw_images.Count; ++i)
					{
						OpenCvSharp.Mat drawed_im = workers[i]._draw_image;
						if (!drawed_im.Empty())
						{
							//OpenCvSharp.Cv2.ImShow(sources_names[i], drawed_im);

							  draw_images[i] = new OpenCvSharp.Mat();
							if (MyConstants.cnt >= 15)
							{
								foreach (Worker w in workers)
								{
									//service.Dispose();
									//video_worker.Dispose(); 
									//w.Dispose();
									//video_worker.Dispose();
									//Main(args);
									int s = w._match_found_callback_id;
									
                                    //string test = w.liveness_2d_res_with_score.ToString();
								}
								//break;
							}

						}


					}
					draw_images_mutex.ReleaseMutex();


				}

				int key = OpenCvSharp.Cv2.WaitKey(20);
				if (27 == key)
				{
					foreach (Worker w in workers)
					{
						w.Dispose();
					}
					break;
				}

				if (' ' == key)
				{
					Console.WriteLine("enable processing 0");
					video_worker.enableProcessingOnStream(0);
				}

				if (13 == key)
				{
					Console.WriteLine("disable processing 0");
					video_worker.disableProcessingOnStream(0);
				}


				if ('r' == key)
				{
					Console.WriteLine("reset trackerOnStream");
					video_worker.resetTrackerOnStream(0);
				}


				// check exceptions in callbacks
				video_worker.checkExceptions();
			}

			//StartServer(args);


			// force free resources
			// otherwise licence error may occur
			// when create sdk object in next time 
			service.Dispose();
			video_worker.Dispose();
		}
		catch (Exception e)
		{
			Console.WriteLine("video_recognition_show exception catched: '{0}'", e.ToString());
			return "Exception";
		}


		return "";


	}

	public static string matchedWith(string matched) {
		
		//for (int i = 0; i < 15; i++) {
			int occurrences = MyConstants.matchedwith.Count(x => x == matched);
		reload(occurrences, matched);
		//}
		Console.WriteLine("************************");
		Console.WriteLine(matched);
		return matched;
	}

	public static void reload(int occurances,string matched) {
		double avgliveness = 0.0;
		string livenessstring = "";
		string matchedwith = "";
		for (int i = 0; i < MyConstants.matchedwith.Length; i++) {
			avgliveness += MyConstants.livescore[i];

			if (i == (MyConstants.livescore.Length - 1)) {
				
				if ((avgliveness / 15) > 0.91)
				{
					if (occurances >= 13) {
						livenessstring = "Real" +" and matched with \t" + matched;
						Console.WriteLine("****************##############");
						Console.WriteLine("REAL");
						Console.WriteLine("****************##############");

						Console.WriteLine(livenessstring);
					}
                    else
                    {
						livenessstring = "Real" + " but not matched  ";
						Console.WriteLine(livenessstring);
					}
				}
				else {
					if (occurances >= 13)
					{
						livenessstring = "Fake" + " matched with \t" + matched;
						Console.WriteLine(livenessstring);
					}else
					{
						livenessstring = "Fake" + " and not matched";
						Console.WriteLine(livenessstring);
					}
					 
				}
			}

		}
	}


	public static async void StartServer(string[] args)
	{
		// Get Host IP Address that is used to establish a connection
		// In this case, we get one IP address of localhost that is IP : 127.0.0.1
		// If a host has multiple addresses, you will get a list of addresses
		IPHostEntry host = Dns.GetHostEntry("localhost");
		IPAddress ipAddress = host.AddressList[0];
		IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000);

		try
		{

			// Create a Socket that will use Tcp protocol
			Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			// A Socket must be associated with an endpoint using the Bind method
			listener.Bind(localEndPoint);
			// Specify how many requests a Socket can listen before it gives Server busy response.
			// We will listen 15 requests at a time
			listener.Listen(15);
			//StartClient();
			Console.WriteLine("Waiting for a connection...");

			string m = faceDec(args);

			string[] b = MyConstants.MyFirstConstant;
			int x = MyConstants.cnt;
			string k = "";

			Socket handler = listener.Accept();

			// Incoming data from the client.
			string data = null;
			byte[] bytes = null;

			while (true)
			{
				bytes = new byte[1024];
				int bytesRec = handler.Receive(bytes);
				data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
				if (data.IndexOf("<EOF>") > -1)
				{
					break;
				}
			}

			Console.WriteLine("Text received : {0}", data);

			byte[] msg = Encoding.ASCII.GetBytes(data);
			handler.Send(msg);
			//handler.Shutdown(SocketShutdown.Both);
			//handler.Close();
		}
		catch (Exception e)
		{
			Console.WriteLine(e.ToString());
		}

		Console.WriteLine("\n Press any key to continue...");
		Console.ReadKey();
	}

	public static void StartClient()
	{
		byte[] bytes = new byte[1024];

		try
		{
			// Connect to a Remote server
			// Get Host IP Address that is used to establish a connection
			// In this case, we get one IP address of localhost that is IP : 127.0.0.1
			// If a host has multiple addresses, you will get a list of addresses
			IPHostEntry host = Dns.GetHostEntry("localhost");
			IPAddress ipAddress = host.AddressList[0];
			IPEndPoint remoteEP = new IPEndPoint(ipAddress, 11000);

			// Create a TCP/IP  socket.
			Socket sender = new Socket(ipAddress.AddressFamily,
				SocketType.Stream, ProtocolType.Tcp);

			// Connect the socket to the remote endpoint. Catch any errors.
			try
			{
				// Connect to Remote EndPoint
				sender.Connect(remoteEP);

				Console.WriteLine("Socket connected to {0}",
					sender.RemoteEndPoint.ToString());

				// Encode the data string into a byte array.
				byte[] msg = Encoding.ASCII.GetBytes("This is a test<EOF>");

				// Send the data through the socket.
				int bytesSent = sender.Send(msg);

				// Receive the response from the remote device.
				int bytesRec = sender.Receive(bytes);
				Console.WriteLine("Echoed test = {0}",
					Encoding.ASCII.GetString(bytes, 0, bytesRec));

				// Release the socket.
				sender.Shutdown(SocketShutdown.Both);
				sender.Close();

			}
			catch (ArgumentNullException ane)
			{
				Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
			}
			catch (SocketException se)
			{
				Console.WriteLine("SocketException : {0}", se.ToString());
			}
			catch (Exception e)
			{
				Console.WriteLine("Unexpected exception : {0}", e.ToString());
			}

		}
		catch (Exception e)
		{
			Console.WriteLine(e.ToString());
		}
	}
}
