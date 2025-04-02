using System;
using System.Collections.Generic;
using System.Linq;

namespace Signify.A1C.Svc.Core.Data.Entities
{
    public sealed class A1CStatusCode
    {
        public static readonly A1CStatusCode A1CPerformed = new A1CStatusCode(1, "A1CPerformed");
        public static readonly A1CStatusCode InventoryUpdateRequested = new A1CStatusCode(2, "InventoryUpdateRequested");
        public static readonly A1CStatusCode InventoryUpdateSuccess = new A1CStatusCode(3, "InventoryUpdateSuccess");
        public static readonly A1CStatusCode InventoryUpdateFail = new A1CStatusCode(4, "InventoryUpdateFail");
        public static readonly A1CStatusCode ValidLabResultsReceived = new A1CStatusCode(5, "ValidLabResultsReceived");
        public static readonly A1CStatusCode BarcodeUpdated = new A1CStatusCode(6, "BarcodeUpdated");
        public static readonly A1CStatusCode BillRequestSent = new A1CStatusCode(7, "BillRequestSent");
        public static readonly A1CStatusCode LabOrderCreated = new A1CStatusCode(8, "LabOrderCreated");
        public static readonly A1CStatusCode InvalidLabResultsReceived = new A1CStatusCode(9, "InvalidLabResultsReceived");
        public static readonly A1CStatusCode A1CNotPerformed = new A1CStatusCode(10, "A1CNotPerformed");

        public int A1CStatusCodeId { get; }
        public string StatusCode { get; }

        public A1CStatusCode()
        {
        }

        public A1CStatusCode(int a1CStatusCodeId, string statusCode)
        {
            A1CStatusCodeId = a1CStatusCodeId;
            StatusCode = statusCode;

        }

        public static readonly List<A1CStatusCode> All =
            new List<A1CStatusCode>(new[]
                {
                    A1CPerformed,
                    InventoryUpdateRequested,
                    InventoryUpdateSuccess,
                    InventoryUpdateFail,
                    ValidLabResultsReceived,
                    BarcodeUpdated,
                    BillRequestSent,
                    LabOrderCreated,
                    InvalidLabResultsReceived,
                    A1CNotPerformed
                }
            );

        public static A1CStatusCode GetA1CStatusCode(string code)
        {
            return All.FirstOrDefault(x => x.StatusCode.Equals(code, StringComparison.OrdinalIgnoreCase));
        }

        public static A1CStatusCode GetA1CStatusCode(int id)
        {
            return All.FirstOrDefault(x => x.A1CStatusCodeId == id);
        }

        public override string ToString()
        {
            return $"{nameof(A1CStatusCodeId)}: {A1CStatusCodeId}, {nameof(StatusCode)}: {StatusCode}";
        }

        public bool Equals(A1CStatusCode other)
        {
            return A1CStatusCodeId == other.A1CStatusCodeId && StatusCode == other.StatusCode;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((A1CStatusCode)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (A1CStatusCodeId * 397) ^ (StatusCode != null ? StatusCode.GetHashCode() : 0);
            }
        }
    }
}
