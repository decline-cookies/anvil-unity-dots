// using Anvil.CSharp.Core;
// using System;
// using System.Collections.Generic;
//
// namespace Anvil.Unity.DOTS.Data
// {
//     public interface IResponseVirtualDataLookup
//     {
//         ResponseJobWriter<TResponse> GetResponseJobWriter<TResponse>()
//             where TResponse : struct;
//     }
//     
//     public class ResponseVirtualDataLookup : AbstractAnvilBase,
//                                              IResponseVirtualDataLookup
//     {
//         private readonly Dictionary<Type, IResponseVirtualData> m_VirtualData = new Dictionary<Type, IResponseVirtualData>();
//
//         public ResponseVirtualDataLookup()
//         {
//         }
//         
//         protected override void DisposeSelf()
//         {
//             foreach (IResponseVirtualData virtualData in m_VirtualData.Values)
//             {
//                 virtualData.Dispose();
//             }
//             base.DisposeSelf();
//         }
//
//         public void CreateResponseVirtualDataForType<TResponse>(IRequestVirtualData<TResponse> requestVirtualData)
//             where TResponse : struct
//         {
//             //TODO: Asserts
//             Type type = typeof(TResponse);
//             ResponseVirtualData<TResponse> responseVirtualData = requestVirtualData.CreateResponseVirtualData();
//             m_VirtualData.Add(type, responseVirtualData);
//         }
//
//         public ResponseVirtualData<TResponse> GetResponseVirtualData<TResponse>()
//             where TResponse : struct
//         {
//             //TODO: Asserts
//             Type type = typeof(TResponse);
//             ResponseVirtualData<TResponse> responseVirtualData = (ResponseVirtualData<TResponse>)m_VirtualData[type];
//             return responseVirtualData;
//         }
//
//         public ResponseJobWriter<TResponse> GetResponseJobWriter<TResponse>()
//             where TResponse : struct
//         {
//             return GetResponseVirtualData<TResponse>().GetResponseJobWriter();
//         }
//     }
// }
