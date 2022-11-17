using System.Runtime.CompilerServices;
using Anvil.CSharp.Mathematics;
using Unity.Mathematics;

namespace Anvil.Unity.DOTS.Mathematics
{
    /// <summary>
    /// A collection of extension methods for working with types under the <see cref="Unity.Mathematics"/> namespace.
    /// (float3, int2, etc..)
    /// </summary>
    public static class MathExtension
    {
        /// <summary>
        /// Get the inverse of a <see cref="float3"/>.
        /// </summary>
        /// <remarks>
        /// Any components that are 0 will invert to <see cref="float.NaN"/>.
        /// Use <see cref="GetInverseSafe" /> if this is a possibility.
        /// </remarks>
        /// <param name="value">The value to get the inverse of.</param>
        /// <returns>The inverted value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 GetInverse(this float3 value)
        {
            return 1f / value;
        }

        /// <summary>
        /// Get the inverse of a <see cref="float3"/> with any <see cref="float.NaN"/> components set to 0
        /// </summary>
        /// <param name="value">The value to get the inverse of.</param>
        /// <returns>The inverted value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 GetInverseSafe(this float3 value)
        {
            float3 inverse = value.GetInverse();
            return math.select(inverse, float3.zero, math.isnan(inverse));
        }

        /// <summary>
        /// Get the scale component from a TRS matrix.
        /// On a LocalToWorld matrix this is the world scale.
        /// </summary>
        /// <param name="matrix">The matrix to evaluate.</param>
        /// <returns>The scale component from the matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 GetScale(this float4x4 matrix)
        {
            float3 scaleMagnitude = GetScaleMagnitude(matrix);
            scaleMagnitude.x *= math.select(1, -1, math.determinant(matrix) < 0);
            return scaleMagnitude;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 GetScaleMagnitude(this float4x4 matrix)
        {
            return new float3(
                math.length(matrix.c0.xyz),
                math.length(matrix.c1.xyz),
                math.length(matrix.c2.xyz));
        }

        /// <summary>
        /// Get the rotation component from a TRS matrix.
        /// On a LocalToWorld matrix this is the world rotation.
        /// </summary>
        /// <param name="matrix">The matrix to evaluate.</param>
        /// <returns>The rotation component from the matrix</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion GetRotation(this float4x4 matrix)
        {
            return new quaternion(math.orthonormalize(new float3x3(matrix)));

            //TODO: #117 - Profile
            // Alternate Implementations
            // 1. Remove scale from matrix
            // float3 scale = matrix.GetScaleMagnitude();
            // if (!math.all(scale.IsApproximately(1f)))
            // {
            //     matrix = math.mul(matrix, float4x4.Scale(scale.GetInverseSafe()));
            // }
            //
            // return new quaternion(matrix);

            //OR
            // 2. Look Rotation Safe
            // return quaternion.LookRotationSafe(matrix.c2.xyz, matrix.c1.xyz);
        }

        /// <summary>
        /// Get the position component from a TRS matrix.
        /// On a LocalToWorld matrix this is the world position.
        /// </summary>
        /// <param name="matrix">The matrix to evaluate.</param>
        /// <returns>The position component from the matrix</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 GetPosition(this float4x4 matrix)
        {
            return new float3(matrix.c0.w, matrix.c1.w, matrix.c2.w);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsApproximately(this float a, float b)
        {
            return math.abs(a - b) < MathUtil.FLOATING_POINT_EQUALITY_TOLERANCE;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool2 IsApproximately(this float2 a, float2 b)
        {
            return math.abs(a - b) < MathUtil.FLOATING_POINT_EQUALITY_TOLERANCE;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool3 IsApproximately(this float3 a, float3 b)
        {
            return math.abs(a - b) < MathUtil.FLOATING_POINT_EQUALITY_TOLERANCE;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool4 IsApproximately(this float4 a, float4 b)
        {
            return math.abs(a - b) < MathUtil.FLOATING_POINT_EQUALITY_TOLERANCE;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool3x3 IsApproximately(this float3x3 a, float3x3 b)
        {
            return new bool3x3(
                a.c0.IsApproximately(b.c0),
                a.c1.IsApproximately(b.c1),
                a.c2.IsApproximately(b.c2)
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool4x4 IsApproximately(this float4x4 a, float4x4 b)
        {
            return new bool4x4(
                a.c0.IsApproximately(b.c0),
                a.c1.IsApproximately(b.c1),
                a.c2.IsApproximately(b.c2),
                a.c3.IsApproximately(b.c3)
            );
        }
    }
}