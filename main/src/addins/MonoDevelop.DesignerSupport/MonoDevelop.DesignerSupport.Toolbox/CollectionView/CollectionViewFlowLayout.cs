﻿using System;
using AppKit;
using CoreGraphics;
using Foundation;
using System.Linq;

namespace MonoDevelop.DesignerSupport.Toolbox
{
	class CollectionViewFlowLayout : NSCollectionViewFlowLayout
	{

	}

	class CollectionViewDelegateFlowLayout : NSCollectionViewDelegateFlowLayout
	{
		public bool IsOnlyImage { get; set; }
		public bool IsShowCategories { get; set; }

		public override CGSize SizeForItem (NSCollectionView collectionView, NSCollectionViewLayout collectionViewLayout, NSIndexPath indexPath)
		{
			var categories = ((CollectionView)collectionView).Categories;
			var category = categories.ElementAt ((int)indexPath.Section);
			var selectedItem = category.Items[(int)indexPath.Item];
			if (!category.IsExpanded || !selectedItem.IsVisible) {
				return new CGSize (0, 0);
			}

			if (IsOnlyImage) {
				return ImageCollectionViewItem.Size;
			}
			var delegateFlowLayout = (CollectionViewFlowLayout)collectionViewLayout;
			var sectionInset = delegateFlowLayout.SectionInset;
			return new CGSize (collectionView.Frame.Width - sectionInset.Right - sectionInset.Left, LabelCollectionViewItem.ItemHeight);
		}

		public override NSEdgeInsets InsetForSection (NSCollectionView collectionView, NSCollectionViewLayout collectionViewLayout, nint section)
		{
			return new NSEdgeInsets (0, 0, 0, 0);
		}

		public override CGSize ReferenceSizeForHeader (NSCollectionView collectionView, NSCollectionViewLayout collectionViewLayout, nint section)
		{
			if (!IsShowCategories) {
				return CGSize.Empty;
			}
			var delegateFlowLayout = ((CollectionViewFlowLayout)collectionViewLayout);
			var sectionInset = delegateFlowLayout.SectionInset;
			return new CGSize (collectionView.Frame.Width, HeaderCollectionViewItem.SectionHeight);
		}

		public override CGSize ReferenceSizeForFooter (NSCollectionView collectionView, NSCollectionViewLayout collectionViewLayout, nint section)
		{
			return CGSize.Empty;
		}

		public override NSSet ShouldDeselectItems (NSCollectionView collectionView, NSSet indexPaths)
		{
			return indexPaths;
		}

		public override NSSet ShouldSelectItems (NSCollectionView collectionView, NSSet indexPaths)
		{
			return indexPaths;
		}
	}
}
