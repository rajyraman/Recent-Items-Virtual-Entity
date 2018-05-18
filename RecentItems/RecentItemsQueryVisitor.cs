using System.Windows;
using Microsoft.Xrm.Sdk.Query;
using RYR.VirtualEntity.RecentItems.Definitions;

namespace RYR.VirtualEntity.RecentItems
{
    public class RecentItemsQueryVisitor : IQueryExpressionVisitor
    {
        public string ConditionAttribute { get; set; }
        public object ConditionValue { get; set; }
        public ConditionOperator ConditionType { get; set; }
        public QueryExpression Visit(QueryExpression query)
        {
            var filter = query.Criteria;
            if (filter.Conditions.Count == 0) return query;

            var userCondition = query.Criteria.Conditions[0];
            ConditionType = userCondition.Operator;

            switch (userCondition.Operator)
            {
                case ConditionOperator.Equal:
                    ConditionAttribute = userCondition.AttributeName;
                    ConditionValue = userCondition.Values[0];
                    break;
                case ConditionOperator.EqualUserId:
                    ConditionAttribute = userCondition.AttributeName;
                    break;
            }
            return query;
        }
    }
}
