﻿using System;
using System.Collections.Generic;
using AppKit;
using CoreGraphics;
using Foundation;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.DesignerSupport.Toolbox
{
	[Register ("CollectionView")]
	class CollectionView : NSCollectionView, IToolboxWidget
	{
		public Action<Gdk.EventButton> DoPopupMenu { get; set; }
		public event EventHandler DragBegin;

		readonly List<ToolboxWidgetCategory> categories = new List<ToolboxWidgetCategory> ();

		CollectionViewDataSource dataSource;
		CollectionViewFlowLayoutDelegate collectionViewDelegate;
		CollectionViewFlowLayout flowLayout;

		public IEnumerable<ToolboxWidgetCategory> Categories {
			get { return categories; }
		}
	
		public override void SetFrameSize (CGSize newSize)
		{
			if (Frame.Size != newSize) {
				flowLayout.InvalidateLayout ();
			}
			base.SetFrameSize (newSize);
		}

		public void HideTooltipWindow ()
		{
			//To implement

		}

		public event EventHandler SelectedItemChanged;
		protected virtual void OnSelectedItemChanged (EventArgs args)
		{
			HideTooltipWindow ();
			SelectedItemChanged?.Invoke (this, args);
		}

		ToolboxWidgetItem selectedItem;
		public ToolboxWidgetItem SelectedItem {
			get {
				return selectedItem;
			}
			set {
				if (selectedItem != value) {
					selectedItem = value;
					ScrollToSelectedItem ();
					OnSelectedItemChanged (EventArgs.Empty);
				}
			}
		}

		IPadWindow container;

		public CollectionView (IPadWindow container) : base ()
		{
			this.container = container;
			container.PadContentShown += OnContainerIsShown;

			Initialize ();
		}

		// Called when created from unmanaged code
		public CollectionView (IntPtr handle) : base (handle)
		{
			Initialize ();
		}

		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public CollectionView (NSCoder coder) : base (coder)
		{
			Initialize ();
		}

		// Shared initialization code
		public void Initialize ()
		{
			flowLayout = new CollectionViewFlowLayout ();
			flowLayout.SectionHeadersPinToVisibleBounds = true;
			flowLayout.MinimumInteritemSpacing = 0;
			flowLayout.MinimumLineSpacing = 0;
			flowLayout.SectionFootersPinToVisibleBounds = false;
			//flowLayout.SectionInset = new NSEdgeInsets(top: 10.0f, left: 20.0f, bottom: 10.0f, right: 20.0f);
			CollectionViewLayout = flowLayout;
		
			Delegate = collectionViewDelegate = new CollectionViewFlowLayoutDelegate ();
			Selectable = true;
			AllowsEmptySelection = true;
			DataSource = dataSource = new CollectionViewDataSource (categories);

			collectionViewDelegate.DragBegin += (s, e) => {
				DragBegin?.Invoke (this, EventArgs.Empty);
			};

			collectionViewDelegate.SelectionChanged += (s, e) => {
				if (e.Count == 0) {
					return;
				}
				if (e.AnyObject is NSIndexPath indexPath) {
					SelectedItem = categories[(int)indexPath.Section].Items[(int)indexPath.Item];
				}
			};
			BackgroundColors = new NSColor[] { Styles.SearchTextFieldLineBackgroundColor };
		}

		//internal void ResizeViews ()
		//{
		//	//InvokeOnMainThread(ReloadData);
		//	//ReloadData();

		//	//if (EnclosingScrollView != null)
		//	//{
		//	//	EnclosingScrollView.NeedsDisplay = NeedsDisplay = true;
		//	//	EnclosingScrollView.LayoutSubtreeIfNeeded();
		//	//	SetFrameSize(CollectionViewLayout.CollectionViewContentSize);
		//	//}
		//}

		public bool IsListMode {
			get => collectionViewDelegate.IsOnlyImage;
			set {
				collectionViewDelegate.IsOnlyImage = dataSource.IsOnlyImage = !value;

				this.QueueResize ();
				this.ScrollToSelectedItem ();
			}
		}

		public bool ShowCategories {
			get => collectionViewDelegate.IsShowCategories;
			set {
				collectionViewDelegate.IsShowCategories = value;
				//if (isShowCategories)
				//{
				//	flowLayout.HeaderReferenceSize = new CGSize(Frame.Width - 10, 20);
				//}
				//else
				//{
				//flowLayout.HeaderReferenceSize = CGSize.Empty;
				//}
				//ReloadData ();
				QueueResize ();
				ScrollToSelectedItem ();
				//ReloadData ();
			}
		}

		public void ScrollToSelectedItem ()
		{
			//to implement
		}

		public IEnumerable<ToolboxWidgetItem> AllItems {
			get {
				foreach (ToolboxWidgetCategory category in this.categories) {
					foreach (ToolboxWidgetItem item in category.Items) {
						yield return item;
					}
				}
			}
		}

		Xwt.Size iconSize = new Xwt.Size (24, 24);

		public void ClearCategories ()
		{
			categories.Clear ();
			iconSize = new Xwt.Size (24, 24);
		}

		public string CustomMessage { get; set; }

		public bool CanIconizeToolboxCategories {
			get {
				foreach (ToolboxWidgetCategory category in categories) {
					if (category.CanIconizeItems)
						return true;
				}
				return false;
			}
		}

		internal void OnContainerIsShown (object sender, EventArgs e)
		{
			RegisterClassForItem (typeof (HeaderCollectionViewItem), HeaderCollectionViewItem.Name);
			RegisterClassForItem (typeof (LabelCollectionViewItem), LabelCollectionViewItem.Name);
			RegisterClassForItem (typeof (ImageCollectionViewItem), ImageCollectionViewItem.Name);
		}

		protected override void Dispose (bool disposing)
		{
			if (container != null) {
				container.PadContentShown -= OnContainerIsShown;
			}
			base.Dispose (disposing);
		}

		public void QueueDraw ()
		{
			//NeedsDisplay = true;
			ReloadData ();
		}

		public void QueueResize ()
		{
			flowLayout.InvalidateLayout ();
		}

		public void AddCategory (ToolboxWidgetCategory category)
		{
			categories.Add (category);
			foreach (ToolboxWidgetItem item in category.Items) {
				if (item.Icon == null)
					continue;

				this.iconSize.Width = Math.Max (this.iconSize.Width, (int)item.Icon.Width);
				this.iconSize.Height = Math.Max (this.iconSize.Height, (int)item.Icon.Height);
			}
		}

		public override NSView MakeSupplementaryView (NSString elementKind, string identifier, NSIndexPath indexPath)
		{
			var item = MakeItem (identifier, indexPath) as HeaderCollectionViewItem;
			if (item == null) {
				return null;
			}

			var toolboxWidgetCategory = categories[(int)indexPath.Section];
			item.ExpandButton.AccessibilityTitle = toolboxWidgetCategory.Tooltip ?? "";
			item.ExpandButton.SetCustomTitle (toolboxWidgetCategory.Text ?? "");
			item.IsCollapsed = flowLayout.SectionAtIndexIsCollapsed ((nuint)indexPath.Section);

			//persisting the expanded value over our models (this is not necessary)
			toolboxWidgetCategory.IsExpanded = !item.IsCollapsed;

			item.ExpandButton.Activated += (sender, e) => {
				ToggleSectionCollapse (item.View);
				item.IsCollapsed = flowLayout.SectionAtIndexIsCollapsed ((nuint)indexPath.Section);
				toolboxWidgetCategory.IsExpanded = !item.IsCollapsed;
				ReloadData ();
			};

			return item.View;
		}
	}
}
