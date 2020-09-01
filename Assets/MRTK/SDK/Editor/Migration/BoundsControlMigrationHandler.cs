// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.MixedReality.Toolkit.UI.BoundsControl;
using Microsoft.MixedReality.Toolkit.UI.BoundsControlTypes;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEditor;
using UnityEngine;
using Boo.Lang;
using System.Collections;

namespace Microsoft.MixedReality.Toolkit.Utilities
{
    /// <summary>
    /// Migration handler for migrating bounding box gameobjects to bounds control gameobjects.
    /// </summary>
    public class BoundsControlMigrationHandler : IMigrationHandler
    {

        private Stack ToUpgradeList = new Stack();

        /// <inheritdoc />
        public bool CanMigrate(GameObject gameObject)
        {
            return gameObject.GetComponent<BoundingBox>() != null;
        }

        private void BuildPrefabUpgradeHierarchy(GameObject prefabRoot, GameObject currentGameObject)
        {
            GameObject parentPrefab = PrefabUtility.GetCorrespondingObjectFromSource(currentGameObject);
            if (parentPrefab != prefabRoot)
            {
                ToUpgradeList.Push(parentPrefab);
                BuildPrefabUpgradeHierarchy(prefabRoot, parentPrefab);
            }
        }

        private void MigratePropertyValues(GameObject gameObject)
        {
            var boundingBox = gameObject.GetComponent<BoundingBox>();
            var boundsControl = gameObject.GetComponent<BoundsControl>();
            boundsControl.enabled = boundingBox.enabled;

            {
                Undo.RecordObject(gameObject, "BoundsControl migration: swapping BoundingBox with BoundsControl.");

                // migrate logic settings
                boundsControl.Target = boundingBox.Target;
                boundsControl.BoundsOverride = boundingBox.BoundsOverride;
                boundsControl.CalculationMethod = MigrateCalculationMethod(boundingBox.CalculationMethod);
                boundsControl.BoundsControlActivation = MigrateActivationFlag(boundingBox.BoundingBoxActivation);

                // only carry over min max scaling values if user hasn't attached min max scale constraint component yet 
                if (gameObject.GetComponent<MinMaxScaleConstraint>() == null)
                {
#pragma warning disable 0618
                    // create a minmaxscaleconstraint in case there's a min max scale set up
                    if (boundingBox.ScaleMinimum != 0.0f || boundingBox.ScaleMaximum != 0.0f)
                    {
                        MinMaxScaleConstraint scaleConstraint = gameObject.AddComponent<MinMaxScaleConstraint>();
                        scaleConstraint.ScaleMinimum = boundingBox.ScaleMinimum;
                        scaleConstraint.ScaleMaximum = boundingBox.ScaleMaximum;
                    }
#pragma warning restore 0618
                }

                // migrate visuals
                boundsControl.FlattenAxis = MigrateFlattenAxis(boundingBox.FlattenAxis);
                boundsControl.BoxPadding = boundingBox.BoxPadding;
                MigrateBoxDisplay(boundsControl, boundingBox);
                MigrateLinks(boundsControl, boundingBox);
                MigrateScaleHandles(boundsControl, boundingBox);
                MigrateRotationHandles(boundsControl, boundingBox);
                MigrateProximityEffect(boundsControl, boundingBox);

                // debug properties
                boundsControl.DebugText = boundingBox.debugText;
                boundsControl.HideElementsInInspector = boundingBox.HideElementsInInspector;

                // events
                boundsControl.RotateStarted = boundingBox.RotateStarted;
                boundsControl.RotateStopped = boundingBox.RotateStopped;
                boundsControl.ScaleStarted = boundingBox.ScaleStarted;
                boundsControl.ScaleStopped = boundingBox.ScaleStopped;
            }

            // look in the scene for app bars and upgrade them too to point to the new component
            MigrateAppBar(boundingBox, boundsControl);
        }
        /// <inheritdoc />
        public void Migrate(GameObject gameObject)
        {
            //string scenePath = gameObject.scene.path;
            var boundingBox = gameObject.GetComponent<BoundingBox>();
            
            // build our upgrade stack
            GameObject componentOrigin = gameObject;
            ToUpgradeList.Push(boundingBox);
            bool isPartOfPRefab = PrefabUtility.IsPartOfAnyPrefab(boundingBox);
            if (isPartOfPRefab)
            {
            Object sourceObject = boundingBox;
                
                //BoundingBox rootPrefabBox = PrefabUtility.GetCorrespondingObjectFromOriginalSource(boundingBox);
                while (sourceObject != null)
                {
                    //string pathToPrefab = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(sourceObject);
                    // Object rootPrefabObject = PrefabUtility.GetCorrespondingObjectFromOriginalSource(sourceObject);//, pathToPrefab);
                    //var rootPrefabObject = PrefabUtility.LoadPrefabContents(rootPrefabObject);
                    var rootPrefabObject = PrefabUtility.GetCorrespondingObjectFromSource(sourceObject);
                    if (rootPrefabObject != null)
                    {
                        ToUpgradeList.Push(rootPrefabObject);
                    }
                    componentOrigin = ((BoundingBox)sourceObject).gameObject;
                    sourceObject = rootPrefabObject;
                }

                //GameObject rootPrefabObject = rootPrefabBox.gameObject;
                //if (rootPrefabObject != gameObject)
                //{
                //    componentOrigin = rootPrefabObject;
                //    BuildPrefabUpgradeHierarchy(rootPrefabObject, gameObject);
                //    ToUpgradeList.Push(rootPrefabObject);
                //}
            }

           // if (isPartOfPRefab)

            {
                while (ToUpgradeList.Count != 0)
                {
                    BoundingBox elementToUpgrade = (BoundingBox)ToUpgradeList.Pop();
                    //PrefabAssetType prefabType = PrefabUtility.GetPrefabAssetType(elementToUpgrade);
                    elementToUpgrade.gameObject.EnsureComponent<BoundsControl>();
                    MigratePropertyValues(elementToUpgrade.gameObject);
                    if (elementToUpgrade != boundingBox)
                    {
                        PrefabUtility.SaveAsPrefabAsset(elementToUpgrade.gameObject, elementToUpgrade.gameObject.scene.path);
                    }
                    //if (prefabType == PrefabAssetType.Regular || prefabType == PrefabAssetType.Variant)
                    //{
                    //    // load prefab root and add component there
                    //    //string assetPath = elementToUpgrade.scene.path;
                    //    string assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(elementToUpgrade);
                    //    var prefabObject = UnityEditor.PrefabUtility.LoadPrefabContents(assetPath);
                    //    if (elementToUpgrade == componentOrigin)
                    //    {
                    //        prefabObject.EnsureComponent<BoundsControl>();
                    //    }
                    //    MigratePropertyValues(prefabObject);
                    //    PrefabUtility.SaveAsPrefabAsset(prefabObject, assetPath);
                    //    PrefabUtility.UnloadPrefabContents(prefabObject);
                    //}
                    //else
                    //{
                    //    if (elementToUpgrade == componentOrigin)
                    //    {
                    //        // add component to gameobject directly
                    //        elementToUpgrade.AddComponent<BoundsControl>();
                    //    }
                    //    MigratePropertyValues(elementToUpgrade);
                   // }
                }
            }


            var originBox = componentOrigin.GetComponent<BoundingBox>();
            if (originBox != boundingBox)
            {
                GameObject originBoxGo = originBox.gameObject;
                Object.DestroyImmediate(originBox);
                PrefabUtility.SaveAsPrefabAsset(originBoxGo, originBoxGo.scene.path);
            }
            else
            {
                Object.DestroyImmediate(originBox);

            }


            //if (componentOrigin != gameObject)
            //{

            //}
            //else
            //{
            //    Object.DestroyImmediate(boundingBox);
            //}
            /*var boundsControl = */

            // upgrade every hierarchy level of our prefab
            //while (ToUpgradeList.Count != 0)
            //{
            //    GameObject elementToUpgrade = (GameObject)ToUpgradeList.Pop();
            //    MigratePropertyValues(elementToUpgrade);

            //    PrefabAssetType prefabType = PrefabUtility.GetPrefabAssetType(elementToUpgrade);
            //    if (prefabType == PrefabAssetType.Regular || prefabType == PrefabAssetType.Variant)
            //    {
            //        string assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(elementToUpgrade);
            //        PrefabUtility.SaveAsPrefabAsset(elementToUpgrade, assetPath);
            //    }
            //}

            
//todo
            //{
            //    Undo.RecordObject(gameObject, "Removing obsolete BoundingBox component");
            //    // destroy obsolete component
            //    if (isPartOfPRefab)
            //    {
            //        // destroy in prefab
            //        var prefabBox = componentOrigin.GetComponent<BoundingBox>();
            //        Object.DestroyImmediate(prefabBox);
            //        PrefabAssetType prefabType = PrefabUtility.GetPrefabAssetType(componentOrigin);
            //        if (prefabType == PrefabAssetType.Regular || prefabType == PrefabAssetType.Variant)
            //        {
            //            string assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(componentOrigin);
            //            PrefabUtility.SaveAsPrefabAsset(componentOrigin, assetPath);
            //        }
            //    }
            //    else
            //    {
            //        Object.DestroyImmediate(boundingBox);
            //    }
            //}
        }

        #region Flags Migration

        private BoundsCalculationMethod MigrateCalculationMethod(BoundingBox.BoundsCalculationMethod calculationMethod)
        {
            switch (calculationMethod)
            {
                case BoundingBox.BoundsCalculationMethod.RendererOverCollider:
                    return BoundsCalculationMethod.RendererOverCollider;
                case BoundingBox.BoundsCalculationMethod.ColliderOverRenderer:
                    return BoundsCalculationMethod.ColliderOverRenderer;
                case BoundingBox.BoundsCalculationMethod.ColliderOnly:
                    return BoundsCalculationMethod.ColliderOnly;
                case BoundingBox.BoundsCalculationMethod.RendererOnly:
                    return BoundsCalculationMethod.RendererOnly;
            }

            Debug.Assert(false, "Tried to migrate unsupported bounds calculation method in bounding box / bounds control");
            return BoundsCalculationMethod.RendererOverCollider;
        }

        private BoundsControlActivationType MigrateActivationFlag(BoundingBox.BoundingBoxActivationType activationFlag)
        {
            switch (activationFlag)
            {
                case BoundingBox.BoundingBoxActivationType.ActivateOnStart:
                    return BoundsControlActivationType.ActivateOnStart;
                case BoundingBox.BoundingBoxActivationType.ActivateByProximity:
                    return BoundsControlActivationType.ActivateByProximity;
                case BoundingBox.BoundingBoxActivationType.ActivateByPointer:
                    return BoundsControlActivationType.ActivateByPointer;
                case BoundingBox.BoundingBoxActivationType.ActivateByProximityAndPointer:
                    return BoundsControlActivationType.ActivateByProximityAndPointer;
                case BoundingBox.BoundingBoxActivationType.ActivateManually:
                    return BoundsControlActivationType.ActivateManually;
            }

            Debug.Assert(false, "Tried to migrate unsupported activation flag in bounding box / bounds control");
            return BoundsControlActivationType.ActivateOnStart;
        }

        private FlattenModeType MigrateFlattenAxis(BoundingBox.FlattenModeType flattenAxisType)
        {
            switch (flattenAxisType)
            {
                case BoundingBox.FlattenModeType.DoNotFlatten:
                    return FlattenModeType.DoNotFlatten;
                case BoundingBox.FlattenModeType.FlattenX:
                    return FlattenModeType.FlattenX;
                case BoundingBox.FlattenModeType.FlattenY:
                    return FlattenModeType.FlattenY;
                case BoundingBox.FlattenModeType.FlattenZ:
                    return FlattenModeType.FlattenZ;
                case BoundingBox.FlattenModeType.FlattenAuto:
                    return FlattenModeType.FlattenAuto;
            }

            Debug.Assert(false, "Tried to migrate unsupported flatten axis type in bounding box / bounds control");
            return FlattenModeType.DoNotFlatten;
        }

        private WireframeType MigrateWireframeShape(BoundingBox.WireframeType wireframeType)
        {
            switch (wireframeType)
            {
                case BoundingBox.WireframeType.Cubic:
                    return WireframeType.Cubic;
                case BoundingBox.WireframeType.Cylindrical:
                    return WireframeType.Cylindrical;
            }

            Debug.Assert(false, "Tried to migrate unsupported wireframe type in bounding box / bounds control");
            return WireframeType.Cubic;
        }

        private HandlePrefabCollider MigrateRotationHandleColliderType(BoundingBox.RotationHandlePrefabCollider rotationHandlePrefabColliderType)
        {
            switch (rotationHandlePrefabColliderType)
            {
                case BoundingBox.RotationHandlePrefabCollider.Sphere:
                    return HandlePrefabCollider.Sphere;
                case BoundingBox.RotationHandlePrefabCollider.Box:
                    return HandlePrefabCollider.Box;
            }

            Debug.Assert(false, "Tried to migrate unsupported rotation handle collider type in bounding box / bounds control");
            return HandlePrefabCollider.Sphere;
        }

        #endregion Flags Migration

        #region Visuals Configuration Migration

        private T EnsureConfiguration<T>(T config) where T : ScriptableObject
        {
            return config == null ? ScriptableObject.CreateInstance<T>() : config;
        }

        private void MigrateBoxDisplay(BoundsControl control, BoundingBox box)
        {
            BoxDisplayConfiguration config = EnsureConfiguration(control.BoxDisplayConfig);
            config.BoxMaterial = box.BoxMaterial;
            config.BoxGrabbedMaterial = box.BoxGrabbedMaterial;
            config.FlattenAxisDisplayScale = box.FlattenAxisDisplayScale;
            control.BoxDisplayConfig = config;
        }

        private void MigrateLinks(BoundsControl control, BoundingBox box)
        {
            LinksConfiguration config = EnsureConfiguration(control.LinksConfig);
            config.WireframeMaterial = box.WireframeMaterial;
            config.WireframeEdgeRadius = box.WireframeEdgeRadius;
            config.WireframeShape = MigrateWireframeShape(box.WireframeShape);
            config.ShowWireFrame = box.ShowWireFrame;
            control.LinksConfig = config;
        }

        private void MigrateScaleHandles(BoundsControl control, BoundingBox box)
        {
            ScaleHandlesConfiguration config = EnsureConfiguration(control.ScaleHandlesConfig);
            config.HandleSlatePrefab = box.ScaleHandleSlatePrefab;
            config.ShowScaleHandles = box.ShowScaleHandles;
            config.HandleMaterial = box.HandleMaterial;
            config.HandleGrabbedMaterial = box.HandleGrabbedMaterial;
            config.HandlePrefab = box.ScaleHandlePrefab;
            config.HandleSize = box.ScaleHandleSize;
            config.ColliderPadding = box.ScaleHandleColliderPadding;
            config.DrawTetherWhenManipulating = box.DrawTetherWhenManipulating;
            config.HandlesIgnoreCollider = box.HandlesIgnoreCollider;
            control.ScaleHandlesConfig = config;
        }

        private void MigrateRotationHandles(BoundsControl control, BoundingBox box)
        {
            RotationHandlesConfiguration config = EnsureConfiguration(control.RotationHandlesConfig);
            config.HandlePrefabColliderType = MigrateRotationHandleColliderType(box.RotationHandlePrefabColliderType);
            config.ShowHandleForX = box.ShowRotationHandleForX;
            config.ShowHandleForY = box.ShowRotationHandleForY;
            config.ShowHandleForZ = box.ShowRotationHandleForZ;
            config.HandleMaterial = box.HandleMaterial;
            config.HandleGrabbedMaterial = box.HandleGrabbedMaterial;
            config.HandlePrefab = box.RotationHandlePrefab;
            config.HandleSize = box.RotationHandleSize;
            config.ColliderPadding = box.RotateHandleColliderPadding;
            config.DrawTetherWhenManipulating = box.DrawTetherWhenManipulating;
            config.HandlesIgnoreCollider = box.HandlesIgnoreCollider;
            control.RotationHandlesConfig = config;
        }

        private void MigrateProximityEffect(BoundsControl control, BoundingBox box)
        {
            ProximityEffectConfiguration config = EnsureConfiguration(control.HandleProximityEffectConfig);
            config.ProximityEffectActive = box.ProximityEffectActive;
            config.ObjectMediumProximity = box.HandleMediumProximity;
            config.ObjectCloseProximity = box.HandleCloseProximity;
            config.FarScale = box.FarScale;
            config.MediumScale = box.MediumScale;
            config.CloseScale = box.CloseScale;
            config.FarGrowRate = box.FarGrowRate;
            config.MediumGrowRate = box.MediumGrowRate;
            config.CloseGrowRate = box.CloseGrowRate;
            control.HandleProximityEffectConfig = config;
        }

        #endregion Visuals Configuration Migration

        private void MigrateAppBar(BoundingBox boundingBox, BoundsControl boundsControl)
        {
            // note: this might be expensive for larger scenes but we don't know where the appbar is 
            // placed in the hierarchy so we have to search the scene for it
            AppBar[] appBars = Object.FindObjectsOfType<AppBar>();
            for (int i = 0; i < appBars.Length; ++i)
            {
                AppBar appBar = appBars[i];
                if (appBar.Target == boundingBox)
                {
                    Undo.RecordObject(appBar, "BoundsControl migration: changed target of app bar.");
                    appBar.Target = boundsControl;
                    EditorUtility.SetDirty(appBar);
                }
            }
        }
    }
}