﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaintCoinach.Text.Nodes {
    public class StaticInteger : IStaticNode {
        private readonly int _Value;

        TagType INode.Tag { get { return TagType.None; } }
        NodeType INode.Type { get { return NodeType.StaticValue; } }
        NodeFlags INode.Flags { get { return NodeFlags.IsStatic; } }
        public int Value { get { return _Value; } }

        public StaticInteger(int value) {
            _Value = value;
        }

        public override string ToString() {
            return Value.ToString();
        }
        public void ToString(StringBuilder builder) {
            builder.Append(Value);
        }

        #region IDecodeNodeStatic Members

        object IStaticNode.Value {
            get { return Value; }
        }

        #endregion
    }
}
