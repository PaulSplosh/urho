//
// Support for bubbling up to C# the virtual methods calls for Setup, Start and Stop in Application
//
// This is done by using an ApplicationProxy in C++ that bubbles up
//
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Urho {
	
	public partial class Application {

		// references needed to prevent GC from collecting callbacks passed to native code
		static ActionIntPtr setupCallback;
		static ActionIntPtr startCallback;
		static ActionIntPtr stopCallback;

		IList<Action> onUpdateList;
		IList<Action> onSceneUpdateList;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void ActionIntPtr (IntPtr value);

		[DllImport ("mono-urho", CallingConvention=CallingConvention.Cdecl)]
		extern static IntPtr ApplicationProxy_ApplicationProxy (IntPtr contextHandle, ActionIntPtr setup, ActionIntPtr start, ActionIntPtr stop);

		/// <summary>
		/// Last created application
		/// </summary>
		public static Application Current { get; private set; }

		/// <summary>
		/// Supports the simple style with callbacks
		/// </summary>
		public Application (Context context) : base (UrhoObjectFlag.Empty)
		{
			if (context == null)
				throw new ArgumentNullException (nameof(context));

			context.AddRef();

			//keep references to callbacks (supposed to be passed to native code) as long as the App is alive
			if (setupCallback == null)
				setupCallback = ProxySetup;
			if (startCallback == null)
				startCallback = ProxyStart;
			if (stopCallback == null)
				stopCallback = ProxyStop;

			handle = ApplicationProxy_ApplicationProxy (context.Handle, setupCallback, startCallback, stopCallback);
			Runtime.RegisterObject (this);
			Current = this;

			UpdateContext = new ListBasedUpdateSynchronizationContext(onUpdateList = new List<Action>());
			SceneUpdateContext = new ListBasedUpdateSynchronizationContext(onSceneUpdateList = new List<Action>());

			SubscribeToUpdate(args =>
				{
					var timeStep = args.TimeStep;
					Update?.Invoke(args);
					ActionManager.Update(timeStep);
					OnUpdate(timeStep);
					UpdateContext.PumpActions();
				});

			SubscribeToSceneUpdate(args =>
				{
					var timeStep = args.TimeStep;
					var scene = args.Scene;
					SceneUpdate?.Invoke(args);
					OnSceneUpdate(timeStep, scene);
					SceneUpdateContext.PumpActions();
				});
		}
	
		public static void SetCustomApplicationCallback(ActionIntPtr setup, ActionIntPtr start, ActionIntPtr stop)
		{
			setupCallback = setup;
			startCallback = start;
			stopCallback = stop;
		}

		public static Application GetApp(IntPtr h)
		{
			return Runtime.LookupObject<Application>(h);
		}

		/// <summary>
		/// SynchronizationContext to execute action on Update cycle
		/// </summary>
		public ListBasedUpdateSynchronizationContext UpdateContext { get; private set; }

		/// <summary>
		/// SynchronizationContext to execute action on SceneUpdate cycle
		/// </summary>
		public ListBasedUpdateSynchronizationContext SceneUpdateContext { get; private set; }

		public event Action<UpdateEventArgs> Update;

		public event Action<SceneUpdateEventArgs> SceneUpdate;

		public static Task Delay(float durationMs)
		{
			var tcs = new TaskCompletionSource<bool>();
			var state = Current.ActionManager.AddAction(new Sequence(new DelayTime(durationMs), new CallFunc(() => tcs.TrySetResult(true))), null);
			return tcs.Task;
		}
		
		static void ProxySetup (IntPtr h)
		{
			GetApp (h).Setup ();
		}

		static void ProxyStart (IntPtr h)
		{
			GetApp (h).Start ();
		}

		static void ProxyStop (IntPtr h)
		{
			GetApp (h).Stop ();
		}

		public virtual void Setup ()
		{
		}

		public virtual void Start ()
		{
			Runtime.Initialize();
		}

		public virtual void Stop ()
		{
			//Engine.DumpResources(true);
		}

		internal ActionManager ActionManager { get; } = new ActionManager();

		protected virtual void OnSceneUpdate(float timeStep, Scene scene) { }

		protected virtual void OnUpdate(float timeStep) { }

		//
		// GetSubsystem helpers
		//
		ResourceCache resourceCache;
		public ResourceCache ResourceCache {
			get {
				if (resourceCache == null)
					resourceCache = new ResourceCache (UrhoObject_GetSubsystem (handle, ResourceCache.TypeStatic.Code));
				return resourceCache;
			}
		}

		UrhoConsole console;
		public UrhoConsole Console {
			get {
				if (console == null)
					console = new UrhoConsole (UrhoObject_GetSubsystem (handle, UrhoConsole.TypeStatic.Code));
				return console;
			}
		}
		
		Network network;
		public Network Network {
			get {
				if (network == null)
					network = new Network (UrhoObject_GetSubsystem (handle, Network.TypeStatic.Code));
				return network;
			}
		}
		
		Time time;
		public Time Time {
			get {
				if (time == null)
					time = new Time (UrhoObject_GetSubsystem (handle, Time.TypeStatic.Code));
				return time;
			}
		}
		
		WorkQueue workQueue;
		public WorkQueue WorkQueue {
			get {
				if (workQueue == null)
					workQueue = new WorkQueue (UrhoObject_GetSubsystem (handle, WorkQueue.TypeStatic.Code));
				return workQueue;
			}
		}
		
		Profiler profiler;
		public Profiler Profiler {
			get {
				if (profiler == null)
					profiler = new Profiler (UrhoObject_GetSubsystem (handle, Profiler.TypeStatic.Code));
				return profiler;
			}
		}
		
		FileSystem fileSystem;
		public FileSystem FileSystem {
			get {
				if (fileSystem == null)
					fileSystem = new FileSystem (UrhoObject_GetSubsystem (handle, FileSystem.TypeStatic.Code));
				return fileSystem;
			}
		}
		
		Log log;
		public Log Log {
			get {
				if (log == null)
					log = new Log (UrhoObject_GetSubsystem (handle, Log.TypeStatic.Code));
				return log;
			}
		}
		
		Input input;
		public Input Input {
			get {
				if (input == null)
					input = new Input (UrhoObject_GetSubsystem (handle, Input.TypeStatic.Code));
				return input;
			}
		}
		
		Audio audio;
		public Audio Audio {
			get {
				if (audio == null)
					audio = new Audio (UrhoObject_GetSubsystem (handle, Audio.TypeStatic.Code));
				return audio;
			}
		}
		
		UI uI;
		public UI UI {
			get {
				if (uI == null)
					uI = new UI (UrhoObject_GetSubsystem (handle, UI.TypeStatic.Code));
				return uI;
			}
		}
		
		Graphics graphics;
		public Graphics Graphics {
			get {
				if (graphics == null)
					graphics = new Graphics (UrhoObject_GetSubsystem (handle, Graphics.TypeStatic.Code));
				return graphics;
			}
		}
		
		Renderer renderer;
		public Renderer Renderer {
			get {
				if (renderer == null)
					renderer = new Renderer (UrhoObject_GetSubsystem (handle, Renderer.TypeStatic.Code));
				return renderer;
			}
		}

		[DllImport ("mono-urho", CallingConvention=CallingConvention.Cdecl)]
		extern static IntPtr Application_GetEngine (IntPtr handle);
		Engine engine;

		public Engine Engine {
			get {
				if (engine == null)
					engine = new Engine (Application_GetEngine (handle));
				return engine;
			}
		}

		public static T CreateInstance<T>() where T : Application
		{
			try
			{
				return (T) Activator.CreateInstance(typeof (T), new Context());
			}
			catch (Exception exc)
			{
				throw new InvalidOperationException($"{typeof(T)} should have a public ctor with a single argument (Context)", exc);
			}
		}
	}
}
