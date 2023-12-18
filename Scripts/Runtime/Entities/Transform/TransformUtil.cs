using System.Diagnostics;
using System.Runtime.CompilerServices;
using Anvil.Unity.Core;
using Anvil.Unity.DOTS.Logging;
using Anvil.Unity.DOTS.Mathematics;
using Anvil.Unity.Logging;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

namespace Anvil.Unity.DOTS.Entities.Transform
{
    /// <summary>
    /// A collection of utilities to help work with and transforming values through matrices.
    /// </summary>
    [BurstCompile]
    public static class TransformUtil
    {
        private static BurstableLogger<FixedString32Bytes> Logger
        {
            get => new BurstableLogger<FixedString32Bytes>(string.Empty);
        }

        /// <summary>
        /// Adds any missing transform components to an <see cref="Entity"/> to express basic translation, rotation, and
        /// scale. If a standard component isn't present it is added with an identity value.
        /// </summary>
        /// <param name="entity">The <see cref="Entity"/> to fill components on.</param>
        /// <param name="entityManager">The <see cref="EntityManager"/> for the <see cref="Entity"/></param>
        public static void AddMissingStandardComponents(Entity entity, EntityManager entityManager)
        {
            Debug.Assert(entityManager.Exists(entity));

            if (!entityManager.HasComponent<LocalTransform>(entity))
            {
                entityManager.AddComponentData(entity,  LocalTransform.Identity);
            }
        }

        /// <summary>
        /// Converts a world position value to the local space expressed by a matrix.
        /// </summary>
        /// <param name="localToWorld">The local to world transformation matrix. (will be inverted)</param>
        /// <param name="point">The world position value to convert.</param>
        /// <returns>The local position value.</returns>
        /// <remarks>
        /// NOTE: If calling frequently it may be more performant to invert the <see cref="LocalToWorld.Value"/> matrix
        /// and call the version of this method that takes a matrix instead.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 ConvertWorldToLocalPoint(LocalToWorld localToWorld, float3 point)
        {
            return ConvertWorldToLocalPoint(math.inverse(localToWorld.Value), point);
        }

        /// <summary>
        /// Converts a world position value to the local space expressed by a matrix.
        /// </summary>
        /// <param name="worldToLocalMtx">The world to local transformation matrix.</param>
        /// <param name="point">The world position value to convert.</param>
        /// <returns>The local position value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 ConvertWorldToLocalPoint(float4x4 worldToLocalMtx, float3 point)
        {
            // If the matrix is invalid it cannot produce reliable transformations and the point is infinite
            if (!worldToLocalMtx.IsValidTransform())
            {
                Logger.Error<FixedString128Bytes>("This transform is invalid. Returning a signed infinite position.");
                return point.ToSignedInfinite();
            }

            return math.transform(worldToLocalMtx, point);
        }

        /// <summary>
        /// Converts a local position value to the world space expressed by a matrix.
        /// </summary>
        /// <param name="localToWorld">The local to world transformation matrix.</param>
        /// <param name="point">The local position value to convert.</param>
        /// <returns>The world position value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 ConvertLocalToWorldPoint(LocalToWorld localToWorld, float3 point)
        {
            return ConvertLocalToWorldPoint(localToWorld.Value, point);
        }

        /// <summary>
        /// Converts a local position value to the world space expressed by a matrix.
        /// </summary>
        /// <param name="localToWorldMtx">The local to world transformation matrix.</param>
        /// <param name="point">The local position value to convert.</param>
        /// <returns>The world position value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 ConvertLocalToWorldPoint(float4x4 localToWorldMtx, float3 point)
        {
            // If the matrix is invalid it cannot produce reliable transformations and the point is infinite
            if (!localToWorldMtx.IsValidTransform())
            {
                Logger.Error<FixedString128Bytes>("This transform is invalid. Returning a signed infinite position.");
                return point.ToSignedInfinite();
            }

            return math.transform(localToWorldMtx, point);
        }

        /// <summary>
        /// Converts a world rotation value to the local space expressed by a matrix.
        ///
        /// NOTE: Transform matrices with negative scale values may produce output inconsistent with the existing
        /// component values. The results are still valid but should be applied in tandem with
        /// <see cref="ConvertWorldToLocalScale"/>.
        /// (transforms with negative scale may be represented by multiple combinations of rotation and scale)
        /// </summary>
        /// <param name="localToWorldMtx">The local to world transformation matrix. (will be inverted)</param>
        /// <param name="rotation">The world rotation value to convert.</param>
        /// <returns>The local rotation value.</returns>
        /// <remarks>
        /// NOTE: If calling frequently it may be more performant to invert the <see cref="LocalToWorld.Value"/> matrix
        /// and call the version of this method that takes a matrix instead.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion ConvertWorldToLocalRotation(LocalToWorld localToWorld, quaternion rotation)
        {
            return ConvertWorldToLocalRotation(math.inverse(localToWorld.Value), rotation);
        }

        /// <summary>
        /// Converts a world rotation value to the local space expressed by a matrix.
        ///
        /// NOTE: Transform matrices with negative scale values may produce output inconsistent with the existing
        /// component values. The results are still valid but should be applied in tandem with
        /// <see cref="ConvertWorldToLocalScale"/>.
        /// (transforms with negative scale may be represented by multiple combinations of rotation and scale)
        /// </summary>
        /// <param name="worldToLocalMtx">The world to local transformation matrix.</param>
        /// <param name="rotation">The world rotation value to convert.</param>
        /// <returns>The local rotation value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion ConvertWorldToLocalRotation(float4x4 worldToLocalMtx, quaternion rotation)
        {
            // If the matrix is invalid it cannot produce reliable transformations and the rotation is 0
            if (!worldToLocalMtx.IsValidTransform())
            {
                Logger.Error("Transform is not valid. Returning identity rotation.");
                return quaternion.identity;
            }

            EmitErrorIfNonUniformScale(worldToLocalMtx.GetScale());

            return quaternion.LookRotationSafe(
                math.rotate(worldToLocalMtx, math.mul(rotation, math.forward())),
                math.rotate(worldToLocalMtx, math.mul(rotation, math.up())));
        }

        /// <summary>
        /// Converts a local rotation value to the world space expressed by a matrix.
        ///
        /// NOTE: Transform matrices with negative scale values may produce output inconsistent with the existing
        /// component values. The results are still valid but should be applied in tandem with
        /// <see cref="ConvertWorldToLocalScale"/>.
        /// (transforms with negative scale may be represented by multiple combinations of rotation and scale)
        /// </summary>
        /// <param name="localToWorld">The local to world transformation matrix.</param>
        /// <param name="rotation">The local rotation value to convert.</param>
        /// <returns>The world rotation value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion ConvertLocalToWorldRotation(LocalToWorld localToWorld, quaternion rotation)
        {
            return ConvertLocalToWorldRotation(localToWorld.Value, rotation);
        }

        /// <summary>
        /// Converts a local rotation value to the world space expressed by a matrix.
        ///
        /// NOTE: Transform matrices with negative scale values may produce output inconsistent with the existing
        /// component values. The results are still valid but should be applied in tandem with
        /// <see cref="ConvertWorldToLocalScale"/>.
        /// (transforms with negative scale may be represented by multiple combinations of rotation and scale)
        /// </summary>
        /// <param name="localToWorldMtx">The local to world transformation matrix.</param>
        /// <param name="rotation">The local rotation value to convert.</param>
        /// <returns>The world rotation value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion ConvertLocalToWorldRotation(float4x4 localToWorldMtx, quaternion rotation)
        {
            // If the matrix is invalid it cannot produce reliable transformations and the rotation is 0
            if (!localToWorldMtx.IsValidTransform())
            {
                Logger.Error("Transform is not valid. Returning identity rotation.");
                return quaternion.identity;
            }

            EmitErrorIfNonUniformScale(localToWorldMtx.GetScale());

            return quaternion.LookRotationSafe(
                math.rotate(localToWorldMtx, math.mul(rotation, math.forward())),
                math.rotate(localToWorldMtx, math.mul(rotation, math.up())));
        }

        /// <summary>
        /// Converts a world scale value to the local space expressed by a matrix.
        ///
        /// NOTE: Transform matrices with negative scale values may produce output inconsistent with the existing
        /// component values. The results are still valid but should be applied in tandem with
        /// <see cref="ConvertWorldToLocalRotation"/>.
        /// (transforms with negative scale may be represented by multiple combinations of rotation and scale)
        /// </summary>
        /// <param name="localToWorld">The local to world transformation matrix. (will be inverted)</param>
        /// <param name="scale">The world scale value transform.</param>
        /// <returns>The local scale value.</returns>
        /// <remarks>
        /// NOTE: If calling frequently it may be more performant to invert the <see cref="LocalToWorld.Value"/> matrix
        /// and call the version of this method that takes a matrix instead.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 ConvertWorldToLocalScale(LocalToWorld localToWorld, float3 scale)
        {
            return ConvertWorldToLocalScale(math.inverse(localToWorld.Value), scale);
        }

        /// <summary>
        /// Converts a world scale value to the local space expressed by a matrix.
        ///
        /// NOTE: Transform matrices with negative scale values may produce output inconsistent with the existing
        /// component values. The results are still valid but should be applied in tandem with
        /// <see cref="ConvertWorldToLocalRotation"/>.
        /// (transforms with negative scale may be represented by multiple combinations of rotation and scale)
        /// </summary>
        /// <param name="worldToLocalMtx">The world to local transformation matrix.</param>
        /// <param name="scale">The world scale value transform.</param>
        /// <returns>The local scale value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 ConvertWorldToLocalScale(float4x4 worldToLocalMtx, float3 scale)
        {
            // If the matrix is invalid cannot it produce reliable transformations and the scale is infinite
            if (!worldToLocalMtx.IsValidTransform())
            {
                Logger.Error<FixedString128Bytes>("This transform is invalid. Returning a signed infinite scale.");
                return scale.ToSignedInfinite();
            }

            float3 worldToLocalScale = worldToLocalMtx.GetScale();
            EmitErrorIfNonUniformScale(worldToLocalScale);

            return worldToLocalScale * scale;
        }

        /// <summary>
        /// Converts a local scale value to the world space expressed by a matrix.
        ///
        /// NOTE: Transform matrices with negative scale values may produce output inconsistent with the existing
        /// component values. The results are still valid but should be applied in tandem with
        /// <see cref="ConvertLocalToWorldScale"/>.
        /// </summary>
        /// <param name="localToWorld">The local to world transformation matrix.</param>
        /// <param name="scale">The local scale value transform.</param>
        /// <returns>The world scale value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 ConvertLocalToWorldScale(LocalToWorld localToWorld, float3 scale)
        {
            return ConvertLocalToWorldScale(localToWorld.Value, scale);
        }

        /// <summary>
        /// Converts a local scale value to the world space expressed by a matrix.
        ///
        /// NOTE: Transform matrices with negative scale values may produce output inconsistent with the existing
        /// component values. The results are still valid but should be applied in tandem with
        /// <see cref="ConvertLocalToWorldScale"/>.
        /// </summary>
        /// <param name="localToWorldMtx">The local to world transformation matrix.</param>
        /// <param name="scale">The local scale value transform.</param>
        /// <returns>The world scale value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 ConvertLocalToWorldScale(float4x4 localToWorldMtx, float3 scale)
        {
            // If the matrix is invalid cannot it produce reliable transformations and the scale is infinite
            if (!localToWorldMtx.IsValidTransform())
            {
                Logger.Error<FixedString128Bytes>("This transform is invalid. Returning a signed infinite scale.");
                return scale.ToSignedInfinite();
            }

            float3 localToWorldScale = localToWorldMtx.GetScale();
            EmitErrorIfNonUniformScale(localToWorldScale);

            return localToWorldScale * scale;
        }

        /// <summary>
        /// Converts a world space <see cref="Rect"/> to a local coordinate space through a <see cref="LocalToWorld"/>
        /// transform.
        /// </summary>
        /// <param name="localToWorld">The transform to apply to the <see cref="Rect"/>.</param>
        /// <param name="worldRect">The world space <see cref="Rect"/> to convert.</param>
        /// <returns>The <see cref="Rect"/> represented in the transform's local space.</returns>
        /// <remarks>
        /// NOTE: Since the <see cref="Rect"/> is a 2D representation being passed through a 3D transformation the resulting
        /// rectangle may not represent the same world area. The resulting rectangle is the 2D world (X/Y) world direction
        /// view of the rectangle after it passes through the transform.
        ///
        /// <example>
        ///     A rectangle that is converted through a transform that rotates 90-degrees on the X-axis will result in a
        ///     height of 0. All of the world height will have been converted to z-depth and is not captured by the
        ///     <see cref="Rect"/> object.
        /// </example>
        /// </remarks>
        public static Rect ConvertWorldToLocalRect(LocalToWorld localToWorld, Rect worldRect)
        {
            //TODO: #119, #118 - Optimize...
            //TODO: #118 - Consider adopting MinMaxAABB or AABB instead (Unity.Mathematics.Extensions + Unity.Mathematics.Extension.Hybrid)

            // If the matrix is invalid it cannot produce reliable transformations and the rect is infinite
            if (!localToWorld.Value.IsValidTransform())
            {
                Logger.Error("This transform is invalid. Returning infinite min/max rect.");
                return Rect.MinMaxRect(float.NegativeInfinity, float.NegativeInfinity, float.PositiveInfinity, float.PositiveInfinity);
            }

            float4x4 worldToLocalMtx = math.inverse(localToWorld.Value);

            float3 point1 = (Vector3)worldRect.min;
            float3 point2 = (Vector3)worldRect.max;
            float3 point3 = new float3(point1.x, point2.y, 0);
            float3 point4 = new float3(point2.x, point1.y, 0);

            return RectUtil.CreateBoundingRect(
                ConvertWorldToLocalPoint(worldToLocalMtx, point1).xy,
                ConvertWorldToLocalPoint(worldToLocalMtx, point2).xy,
                ConvertWorldToLocalPoint(worldToLocalMtx, point3).xy,
                ConvertWorldToLocalPoint(worldToLocalMtx, point4).xy);
        }

        /// <summary>
        /// Converts a local space <see cref="Rect"/> to a world coordinate space through a <see cref="LocalToWorld"/>
        /// transform.
        /// </summary>
        /// <param name="localToWorld">The transform invert and apply to the <see cref="Rect"/>.</param>
        /// <param name="localRect">The local space <see cref="Rect"/> to convert.</param>
        /// <returns>The <see cref="Rect"/> represented in the transform's world space.</returns>
        /// <remarks>
        /// NOTE: Since the <see cref="Rect"/> is a 2D representation being passed through a 3D transformation the resulting
        /// rectangle may not represent the same local area. The resulting rectangle is the 2D (X/Y) world direction
        /// view of the rectangle after it passes through the transform.
        ///
        /// <example>
        ///     A rectangle that is converted through a transform that rotates 90-degrees on the X-axis will result in a
        ///     height of 0. All of the local height will have been converted to z-depth and is not captured by the
        ///     <see cref="Rect"/> object.
        /// </example>
        /// </remarks>
        public static Rect ConvertLocalToWorldRect(LocalToWorld localToWorld, Rect localRect)
        {
            //TODO: #119, #118 - Optimize...
            //TODO: #118 - Consider adopting MinMaxAABB or AABB instead (Unity.Mathematics.Extensions + Unity.Mathematics.Extension.Hybrid)

            // If the matrix is invalid it cannot produce reliable transformations and the rect is infinite
            if (!localToWorld.Value.IsValidTransform())
            {
                Logger.Error("This transform is invalid. Returning infinite min/max rect.");
                return Rect.MinMaxRect(float.NegativeInfinity, float.NegativeInfinity, float.PositiveInfinity, float.PositiveInfinity);
            }

            float3 point1 = (Vector3)localRect.min;
            float3 point2 = (Vector3)localRect.max;
            float3 point3 = new float3(point1.x, point2.y, 0);
            float3 point4 = new float3(point2.x, point1.y, 0);

            return RectUtil.CreateBoundingRect(
                ConvertLocalToWorldPoint(localToWorld.Value, point1).xy,
                ConvertLocalToWorldPoint(localToWorld.Value, point2).xy,
                ConvertLocalToWorldPoint(localToWorld.Value, point3).xy,
                ConvertLocalToWorldPoint(localToWorld.Value, point4).xy);
        }

        //TODO: #116 - Transforms with non-uniform scale operations are not currently supported.
        [Conditional("DEBUG")]
        [UnityLogListener.Exclude]
        private static void EmitErrorIfNonUniformScale(float3 scale)
        {
            scale = math.abs(scale);
            bool isUniform = scale.x.IsApproximately(scale.y) && scale.y.IsApproximately(scale.z);
            if (!isUniform)
            {
                Logger.Error<FixedString128Bytes>("This conversion does not support transforms with non-uniform scaling.");
            }
        }
    }
}