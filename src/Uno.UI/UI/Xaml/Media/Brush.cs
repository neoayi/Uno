﻿using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Media;
using System.ComponentModel;

namespace Windows.UI.Xaml.Media
{
	[TypeConverter(typeof(BrushConverter))]
	public partial class Brush : DependencyObject
	{
		public Brush()
		{
			InitializeBinder();
		}

		public static implicit operator Brush(Color uiColor)
		{
			Color color = uiColor;

			return SolidColorBrushHelper.FromARGB(color.A, color.R, color.G, color.B);
		}

		#region Opacity Dependency Property

		public double Opacity
		{
			get { return (double)this.GetValue(OpacityProperty); }
			set { this.SetValue(OpacityProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Opacity.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty OpacityProperty =
			DependencyProperty.Register(
				"Opacity", 
				typeof(double), 
				typeof(Brush),
				new PropertyMetadata(
					defaultValue: 1d,
					propertyChangedCallback: (s, e) => ((Brush)s).OnOpacityChanged((double)e.OldValue, (double)e.NewValue)
				)
			);

		protected virtual void OnOpacityChanged(double oldValue, double newValue)
		{
		}

		#endregion

		public Transform RelativeTransform
		{
			get { return (Transform)this.GetValue(RelativeTransformProperty); }
			set { this.SetValue(RelativeTransformProperty, value); }
		}

		public static readonly DependencyProperty RelativeTransformProperty =
			DependencyProperty.Register("RelativeTransform", typeof(Transform), typeof(Brush), new PropertyMetadata(null,

                    propertyChangedCallback: (s, e) => ((Brush)s).OnRelativeTransformChanged((Transform)e.OldValue, (Transform)e.NewValue)));

        protected virtual void OnRelativeTransformChanged(Transform oldValue, Transform newValue)
        {
        }
    }
}
