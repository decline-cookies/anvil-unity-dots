using System.Runtime.CompilerServices;
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
        /// Get the scale component from a TRS matrix.
        /// On a LocalToWorld matrix this is the world scale.
        /// </summary>
        /// <param name="matrix">The matrix to evaluate.</param>
        /// <returns>The scale component from the matrix.</returns>
        /// <remarks>
        /// This valid is only valid when used in tandem with <see cref="GetRotation"/> as multiple combinations of scale
        /// and rotation may be used to represent the same transform.
        /// </remarks>
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
        /// On a <see cref="LocalToWorld"/> matrix this is the world rotation.
        /// </summary>
        /// <param name="matrix">The matrix to evaluate.</param>
        /// <returns>The rotation component from the matrix</returns>
        /// <remarks>
        /// This valid is only valid when used in tandem with <see cref="GetScale"/> as multiple combinations of scale
        /// and rotation may be used to represent the same transform.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion GetRotation(this float4x4 matrix)
        {
            return quaternion.LookRotationSafe(matrix.c2.xyz, matrix.c1.xyz);

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
            // 2. Orthonormalize quaternion
            // return new quaternion(math.orthonormalize(new float3x3(matrix)));
        }

        /// <summary>
        /// Get the position component from a TRS matrix.
        /// On a LocalToWorld matrix this is the world position.
        /// </summary>
        /// <param name="matrix">The matrix to evaluate.</param>
        /// <returns>The position component from the matrix</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 GetTranslation(this float4x4 matrix)
        {
            return matrix.c3.xyz;
        }

        /// <summary>
        /// Checks whether a given transformation matrix is valid.
        /// </summary>
        /// <param name="matrix"></param>
        /// <returns></returns>
        /// <remarks>
        /// Returns false if:
        ///  - The determinant is 0 or <see cref="float.NaN"/>.
        ///
        /// An invalid matrix (Ex: with a 0 scale component) cannot be inverted. Special case values must be used when
        /// transforming through an invalid matrix.
        /// </remarks>
        public static bool IsValidTransform(this float4x4 matrix)
        {
            return math.determinant(matrix) is not 0 and not float.NaN;
        }
    }
}
