﻿namespace Raven.Studio.Shell
{
	using System;
	using System.ComponentModel.Composition;
	using System.Linq;
	using System.Windows;
	using Caliburn.Micro;
	using Features.Database;
	using Framework;
	using Framework.Extensions;
	using Messages;
	using Plugins;

	[Export(typeof(IShell))]
	[PartCreationPolicy(CreationPolicy.Shared)]
	public class ShellViewModel : Conductor<IScreen>.Collection.OneActive, IShell,
								  IHandle<DisplayCurrentDatabaseRequested>
	{
		readonly BusyStatusViewModel busyStatus;
		private readonly SelectDatabaseViewModel startScreen;
		private readonly DatabaseExplorer databaseScreen;
		readonly IEventAggregator events;
		readonly NavigationViewModel navigation;
		readonly NotificationsViewModel notifications;
		readonly IServer server;
		readonly IKeyboardShortcutBinder binder;
		readonly Uri serverUri;

		[ImportingConstructor]
		public ShellViewModel(
			IServer server,
			ServerUriProvider uriProvider,
			NavigationViewModel navigation,
			NotificationsViewModel notifications,
			BusyStatusViewModel busyStatus,
			SelectDatabaseViewModel startScreen,
			DatabaseExplorer databaseScreen,
			IKeyboardShortcutBinder binder,
			IEventAggregator events)
		{
			this.databaseScreen = databaseScreen;
			this.navigation = navigation;
			this.notifications = notifications;
			this.busyStatus = busyStatus;
			this.startScreen = startScreen;
			navigation.SetGoHome(() => this.TrackNavigationTo(startScreen, events));
			this.binder = binder;
			this.events = events;
			this.server = server;
			events.Subscribe(this);


			Items.Add(startScreen);
			Items.Add(databaseScreen);

			serverUri = new Uri(uriProvider.GetServerUri());

			events.Publish(new WorkStarted("Connecting to server"));
			server.Connect(serverUri,
						   () =>
						   {
							   events.Publish(new WorkCompleted("Connecting to server"));

							   switch (server.Databases.Count())
							   {
								   case 0:
										//NOTE: perhaps we should display a retry button?
									   break;
								   case 1:
									   ActivateItem(databaseScreen);
									   break;
								   default:
									   ActivateItem(startScreen);
									   break;
							   }
						   });
		}

		public SelectDatabaseViewModel StartScreen
		{
			get { return startScreen; }
		}

		public DatabaseExplorer DatabaseScreen
		{
			get { return databaseScreen; }
		}

		public IServer Server { get { return server; } }

		protected override void OnViewAttached(object view, object context)
		{
			binder.Initialize((FrameworkElement)view);
			base.OnViewAttached(view, context);
		}

		public BusyStatusViewModel BusyStatus { get { return busyStatus; } }

		public NotificationsViewModel Notifications { get { return notifications; } }

		public NavigationViewModel Navigation { get { return navigation; } }

		public Window Window { get { return Application.Current.MainWindow; } }

		public bool ShouldDisplaySystemButtons
		{
			get { return Application.Current.IsRunningOutOfBrowser; }
		}

		void IHandle<DisplayCurrentDatabaseRequested>.Handle(DisplayCurrentDatabaseRequested message)
		{
			//TODO: record the previous database so that the back button is more intuitive
			this.TrackNavigationTo(databaseScreen, events);

			navigation.Breadcrumbs.Clear();
			navigation.Breadcrumbs.Add(databaseScreen);
		}

		public void CloseWindow() { Window.Close(); }

		public void ToggleWindow()
		{
			if (!Application.Current.IsRunningOutOfBrowser) return;

			Window.WindowState = Window.WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal;
		}

		public void MinimizeWindow() { Window.WindowState = WindowState.Minimized; }

		public void DragWindow()
		{
			if (!Application.Current.IsRunningOutOfBrowser) return;

			Window.DragMove();
		}

		public void ResizeWindow(string direction)
		{
			if (!Application.Current.IsRunningOutOfBrowser) return;

			WindowResizeEdge edge;
			Enum.TryParse(direction, out edge);
			Window.DragResize(edge);
		}
	}
}