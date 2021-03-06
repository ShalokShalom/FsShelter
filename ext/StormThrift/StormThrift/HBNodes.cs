/**
 * Autogenerated by Thrift Compiler (0.9.1)
 *
 * DO NOT EDIT UNLESS YOU ARE SURE THAT YOU KNOW WHAT YOU ARE DOING
 *  @generated
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Thrift;
using Thrift.Collections;
using System.Runtime.Serialization;
using Thrift.Protocol;
using Thrift.Transport;

namespace StormThrift
{

  #if !SILVERLIGHT
  [Serializable]
  #endif
  public partial class HBNodes : TBase
  {
    private List<string> _pulseIds;

    public List<string> PulseIds
    {
      get
      {
        return _pulseIds;
      }
      set
      {
        __isset.pulseIds = true;
        this._pulseIds = value;
      }
    }


    public Isset __isset;
    #if !SILVERLIGHT
    [Serializable]
    #endif
    public struct Isset {
      public bool pulseIds;
    }

    public HBNodes() {
    }

    public void Read (TProtocol iprot)
    {
      TField field;
      iprot.ReadStructBegin();
      while (true)
      {
        field = iprot.ReadFieldBegin();
        if (field.Type == TType.Stop) { 
          break;
        }
        switch (field.ID)
        {
          case 1:
            if (field.Type == TType.List) {
              {
                PulseIds = new List<string>();
                TList _list365 = iprot.ReadListBegin();
                for( int _i366 = 0; _i366 < _list365.Count; ++_i366)
                {
                  string _elem367 = null;
                  _elem367 = iprot.ReadString();
                  PulseIds.Add(_elem367);
                }
                iprot.ReadListEnd();
              }
            } else { 
              TProtocolUtil.Skip(iprot, field.Type);
            }
            break;
          default: 
            TProtocolUtil.Skip(iprot, field.Type);
            break;
        }
        iprot.ReadFieldEnd();
      }
      iprot.ReadStructEnd();
    }

    public void Write(TProtocol oprot) {
      TStruct struc = new TStruct("HBNodes");
      oprot.WriteStructBegin(struc);
      TField field = new TField();
      if (PulseIds != null && __isset.pulseIds) {
        field.Name = "pulseIds";
        field.Type = TType.List;
        field.ID = 1;
        oprot.WriteFieldBegin(field);
        {
          oprot.WriteListBegin(new TList(TType.String, PulseIds.Count));
          foreach (string _iter368 in PulseIds)
          {
            oprot.WriteString(_iter368);
          }
          oprot.WriteListEnd();
        }
        oprot.WriteFieldEnd();
      }
      oprot.WriteFieldStop();
      oprot.WriteStructEnd();
    }

    public override string ToString() {
      StringBuilder sb = new StringBuilder("HBNodes(");
      sb.Append("PulseIds: ");
      sb.Append(PulseIds);
      sb.Append(")");
      return sb.ToString();
    }

  }

}
