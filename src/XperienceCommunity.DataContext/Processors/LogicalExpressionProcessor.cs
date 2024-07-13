using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using CMS.ContentEngine;
using XperienceCommunity.DataContext.Interfaces;

namespace XperienceCommunity.DataContext.Processors
{
    internal class LogicalExpressionProcessor : IExpressionProcessor<BinaryExpression>
    {
        private readonly QueryParameterManager _parameterManager;
        private readonly bool _isAnd;

        public LogicalExpressionProcessor(QueryParameterManager parameterManager, bool isAnd)
        {
            _parameterManager = parameterManager;
            _isAnd = isAnd;
        }

        public void Process(BinaryExpression node)
        {

            if (_isAnd)
            {
                _parameterManager.AddLogicalCondition("AND");
            }
            else
            {
                _parameterManager.AddLogicalCondition("OR");
            }
        }
    }
}
