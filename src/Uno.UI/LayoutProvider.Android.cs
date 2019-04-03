using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Android.App;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Util;
using Android.Views;
using Android.Widget;
using Uno.Extensions;
using Uno.Logging;
using Windows.Foundation;
using Rect = Android.Graphics.Rect;

namespace Uno.UI
{
	public class LayoutProvider
	{
		public delegate void LayoutChangedListener(Rect statusBar, Rect keyboard, Rect navigationBar);
		public delegate void DebugLayoutChangedListener(Activity activity, PopupWindow adjustNothing, PopupWindow adjustResize);

		public static LayoutProvider Instance { get; private set; }

		public event LayoutChangedListener LayoutChanged;
		public event DebugLayoutChangedListener DebugLayoutChanged;

		public Rect StatusBarLayout { get; private set; } = new Rect(0, 0, 0, 0);
		public Rect KeyboardLayout { get; private set; } = new Rect(0, 0, 0, 0);
		public Rect NavigationBarLayout { get; private set; } = new Rect(0, 0, 0, 0);

		private readonly Activity _activity;
		private readonly GlobalLayoutProvider _adjustNothingLayoutProvider, _adjustResizeLayoutProvider;

		internal LayoutProvider(Activity activity)
		{
			this._activity = activity;

			_adjustNothingLayoutProvider = new GlobalLayoutProvider(activity, null)
			{
				SoftInputMode = SoftInput.AdjustNothing | SoftInput.StateUnchanged,
				InputMethodMode = InputMethod.NotNeeded,
			};
			_adjustResizeLayoutProvider = new GlobalLayoutProvider(activity, MeasureLayout)
			{
				SoftInputMode = SoftInput.AdjustResize | SoftInput.StateUnchanged,
				InputMethodMode = InputMethod.Needed,
			};

			Instance = this;
		}

		// lifecycle management
		internal void Start(View view)
		{
			if (view?.WindowToken != null)
			{
				_adjustNothingLayoutProvider.Start(view);
				_adjustResizeLayoutProvider.Start(view);
			}
		}
		internal void Stop()
		{
			_adjustNothingLayoutProvider.Stop();
			_adjustResizeLayoutProvider.Stop();
		}

		private void MeasureLayout(PopupWindow sender)
		{
			// We can obtain the size of keyboard by comparing the layout of two popup windows
			// where one (AdjustResize) resizes to keyboard and one(AdjustNothing) that doesn't:
			// [size] realMetrics			: screen
			// [rect] adjustNothingFrame	: screen - (top: status_bar) - (bottom: nav_bar)
			// [rect] adjustResizeFrame		: screen - (top: status_bar) - (bottom: keyboard + nav_bar)
			var realMetrics = Get<DisplayMetrics>(_activity.WindowManager.DefaultDisplay.GetRealMetrics);
			var adjustNothingFrame = Get<Rect>(_adjustNothingLayoutProvider.ContentView.GetWindowVisibleDisplayFrame);
			var adjustResizeFrame = Get<Rect>(_adjustResizeLayoutProvider.ContentView.GetWindowVisibleDisplayFrame);

			StatusBarLayout = new Rect(0, 0, realMetrics.WidthPixels, adjustNothingFrame.Top);
			KeyboardLayout = new Rect(0, adjustResizeFrame.Bottom, realMetrics.WidthPixels, adjustNothingFrame.Bottom);
			NavigationBarLayout = new Rect(0, adjustNothingFrame.Bottom, realMetrics.WidthPixels, realMetrics.HeightPixels);

			LayoutChanged?.Invoke(StatusBarLayout, KeyboardLayout, NavigationBarLayout);

			if (FeatureConfiguration.LayoutProvider.DebugLayout)
			{
				var decorView = _activity.Window.DecorView;
				var flags = _activity.Window.Attributes.Flags;

				var isStatusBarVisible = ((int)decorView.SystemUiVisibility & (int)SystemUiFlags.Fullscreen) == 0;
				var hasTranslucentStatus = flags.HasFlag(WindowManagerFlags.TranslucentStatus);
				var hasTranslucentNavigation = flags.HasFlag(WindowManagerFlags.TranslucentNavigation);
				var hasLayoutNoLimits = flags.HasFlag(WindowManagerFlags.LayoutNoLimits);

				var props = string.Join(",\n", new Dictionary<string, string>
				{
					// measured values
					[nameof(realMetrics)] = Jsonify(realMetrics),
					[nameof(adjustNothingFrame)] = Jsonify(adjustNothingFrame),
					[nameof(adjustResizeFrame)] = Jsonify(adjustResizeFrame),
					// computed values
					[nameof(StatusBarLayout)] = Jsonify(StatusBarLayout),
					[nameof(KeyboardLayout)] = Jsonify(KeyboardLayout),
					[nameof(NavigationBarLayout)] = Jsonify(NavigationBarLayout),
					// for debugging
					[nameof(isStatusBarVisible)] = Jsonify(isStatusBarVisible),
					[nameof(flags)] = Jsonify(flags),
					[nameof(hasTranslucentStatus)] = Jsonify(hasTranslucentStatus),
					[nameof(hasTranslucentNavigation)] = Jsonify(hasTranslucentNavigation),
					[nameof(hasLayoutNoLimits)] = Jsonify(hasLayoutNoLimits),
				}.Select(x => string.Concat("  ", Regex.Replace(x.Key, @"^\w", c => c.Value.ToLower()), ": ", x.Value)));
				this.Log().Debug($"Android layout has been updated: {{\n{props}\n}}");

				DebugLayoutChanged?.Invoke(_activity, _adjustNothingLayoutProvider, _adjustResizeLayoutProvider);
			}
		}

		// helper methods
		private T Get<T>(Action<T> getter) where T : new()
		{
			var result = new T();
			getter(result);

			return result;
		}
		private string Jsonify(DisplayMetrics x) => $"{{ width: {x.WidthPixels}, height: {x.HeightPixels} }}";
		private string Jsonify(Rect x) => $"{{ left: {x.Left}, top: {x.Top}, right: {x.Right}, bottom: {x.Bottom} }}";
		private string Jsonify(bool x) => x.ToString().ToLower();
		private string Jsonify(Enum x) => $"\"{x}\"";


		private class GlobalLayoutProvider : PopupWindow, ViewTreeObserver.IOnGlobalLayoutListener
		{
			public delegate void GlobalLayoutListener(PopupWindow sender);

			private readonly GlobalLayoutListener _listener;
			private readonly Activity _activity;

			public GlobalLayoutProvider(Activity activity, GlobalLayoutListener listener) : base(activity)
			{
				this._activity = activity;
				this._listener = listener;

				ContentView = new LinearLayout(_activity.BaseContext)
				{
					LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
				};
				Width = 0; // this make sure we don't block touch events
				Height = ViewGroup.LayoutParams.MatchParent;
				SetBackgroundDrawable(new ColorDrawable(Color.Transparent));
			}

			// lifecycle management
			public void Start(View view)
			{
				if (!IsShowing)
				{
					ShowAtLocation(view, GravityFlags.NoGravity, 0, 0);
					ContentView.ViewTreeObserver.AddOnGlobalLayoutListener(this);
				}
			}
			public void Stop()
			{
				if (IsShowing)
				{
					Dismiss();
					ContentView.ViewTreeObserver.RemoveOnGlobalLayoutListener(this);
				}
			}

			// event hook
			public void OnGlobalLayout() => _listener?.Invoke(this);
		}
	}
}
