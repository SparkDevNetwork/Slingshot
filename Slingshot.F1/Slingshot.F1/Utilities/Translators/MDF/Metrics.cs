using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using OrcaMDF.Core.MetaData;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace Slingshot.F1.Utilities.Translators
{
    public static class F1Headcount
    {
        /// <summary>
        /// Translates the headcount metrics.
        /// </summary>
        /// <param name="tableData">The table data.</param>
        /// <param name="totalRows">The total rows.</param>
        private int TranslateMetrics( IQueryable<Row> tableData, long totalRows = 0 )
        {
            // Required variables
            var lookupContext = new RockContext();
            var metricService = new MetricService( lookupContext );
            var metricCategoryService = new MetricCategoryService( lookupContext );
            var categoryService = new CategoryService( lookupContext );
            var metricSourceTypes = DefinedTypeCache.Get( new Guid( Rock.SystemGuid.DefinedType.METRIC_SOURCE_TYPE ) ).DefinedValues;
            var metricManualSource = metricSourceTypes.FirstOrDefault( m => m.Guid == new Guid( Rock.SystemGuid.DefinedValue.METRIC_SOURCE_VALUE_TYPE_MANUAL ) );

            var archivedScheduleCategory = GetCategory( lookupContext, ScheduleEntityTypeId, null, "Archived Schedules" );

            var scheduleService = new ScheduleService( lookupContext );
            var scheduleMetrics = scheduleService.Queryable().AsNoTracking().Where( s => s.Category.Guid == archivedScheduleCategory.Guid ).ToList();

            var allMetrics = metricService.Queryable().AsNoTracking().ToList();
            var metricCategories = categoryService.Queryable().AsNoTracking()
                .Where( c => c.EntityType.Guid == new Guid( Rock.SystemGuid.EntityType.METRICCATEGORY ) ).ToList();

            var defaultCategoryName = "Metrics";
            var defaultCategory = metricCategories.FirstOrDefault( c => c.Name == defaultCategoryName );
            if ( defaultCategory == null )
            {
                defaultCategory = GetCategory( lookupContext, MetricCategoryEntityTypeId, null, defaultCategoryName );
                defaultCategory.ForeignKey = string.Format( "Category imported {0}", ImportDateTime );
                metricCategories.Add( defaultCategory );
            }

            var metricValues = new List<MetricValue>();
            Metric currentMetric = null;

            var completedItems = 0;
            var percentage = ( totalRows - 1 ) / 100 + 1;
            ReportProgress( 0, string.Format( "Starting metrics import ({0:N0} already exist).", 0 ) );

            foreach ( var row in tableData.Where( r => r != null ) )
            {
                var foreignId = row["Headcount_ID"] as int?;
                var activityId = row["Activity_ID"] as int?;
                var rlcId = row["RLC_ID"] as int?;
                var metricName = row["RLC_name"] as string;
                var valueDate = row["Start_Date_Time"] as DateTime?;
                var value = row["Attendance"] as string;
                var metricNote = row["Meeting_note"] as string;
                int? metricCampusId = null;

                if ( !string.IsNullOrEmpty( metricName ) && !string.IsNullOrWhiteSpace( value ) )
                {
                    var categoryName = string.Empty;
                    var metricCategoryId = defaultCategory.Id;
                    if ( activityId.HasValue )
                    {
                        var activityGroup = ImportedGroups.FirstOrDefault( g => g.ForeignId == activityId );
                        if ( activityGroup != null && !string.IsNullOrWhiteSpace( activityGroup.Name ) )
                        {
                            metricCampusId = metricCampusId ?? GetCampusId( activityGroup.Name );
                            var activityCategory = metricCategories.FirstOrDefault( c => c.Name == activityGroup.Name && c.ParentCategoryId == metricCategoryId );
                            if ( activityCategory == null )
                            {
                                activityCategory = GetCategory( lookupContext, MetricCategoryEntityTypeId, metricCategoryId, activityGroup.Name );
                                activityCategory.ForeignKey = string.Format( "Category imported {0}", ImportDateTime );
                                metricCategories.Add( activityCategory );
                            }

                            metricCategoryId = activityCategory.Id;
                        }
                    }

                    if ( rlcId.HasValue )
                    {
                        var rlcGroup = ImportedGroups.FirstOrDefault( g => g.ForeignId == rlcId );
                        if ( rlcGroup != null && !string.IsNullOrWhiteSpace( rlcGroup.Name ) )
                        {
                            metricCampusId = metricCampusId ?? GetCampusId( rlcGroup.Name );
                            var rlcCategory = metricCategories.FirstOrDefault( c => c.Name == rlcGroup.Name && c.ParentCategoryId == metricCategoryId );
                            if ( rlcCategory == null )
                            {
                                rlcCategory = GetCategory( lookupContext, MetricCategoryEntityTypeId, metricCategoryId, rlcGroup.Name );
                                rlcCategory.ForeignKey = string.Format( "Category imported {0}", ImportDateTime );
                                metricCategories.Add( rlcCategory );
                            }

                            metricCategoryId = rlcCategory.Id;
                        }
                    }

                    // create metric if it doesn't exist
                    currentMetric = allMetrics.FirstOrDefault( m => m.Title == metricName && m.MetricCategories.Any( c => c.CategoryId == metricCategoryId ) );
                    if ( currentMetric == null )
                    {
                        currentMetric = new Metric();
                        currentMetric.Title = metricName;
                        currentMetric.IsSystem = false;
                        currentMetric.IsCumulative = false;
                        currentMetric.SourceSql = string.Empty;
                        currentMetric.Subtitle = string.Empty;
                        currentMetric.Description = string.Empty;
                        currentMetric.IconCssClass = string.Empty;
                        currentMetric.SourceValueTypeId = metricManualSource.Id;
                        currentMetric.CreatedByPersonAliasId = ImportPersonAliasId;
                        currentMetric.CreatedDateTime = ImportDateTime;
                        currentMetric.ForeignId = foreignId;
                        currentMetric.ForeignKey = foreignId.ToStringSafe();

                        currentMetric.MetricPartitions = new List<MetricPartition>();
                        currentMetric.MetricPartitions.Add( new MetricPartition
                        {
                            Label = "Campus",
                            Metric = currentMetric,
                            EntityTypeId = CampusEntityTypeId,
                            EntityTypeQualifierColumn = string.Empty,
                            EntityTypeQualifierValue = string.Empty
                        } );

                        currentMetric.MetricPartitions.Add( new MetricPartition
                        {
                            Label = "Service",
                            Metric = currentMetric,
                            EntityTypeId = ScheduleEntityTypeId,
                            EntityTypeQualifierColumn = string.Empty,
                            EntityTypeQualifierValue = string.Empty
                        } );

                        metricService.Add( currentMetric );
                        lookupContext.SaveChanges();

                        if ( currentMetric.MetricCategories == null || !currentMetric.MetricCategories.Any( a => a.CategoryId == metricCategoryId ) )
                        {
                            metricCategoryService.Add( new MetricCategory { CategoryId = metricCategoryId, MetricId = currentMetric.Id } );
                            lookupContext.SaveChanges();
                        }

                        allMetrics.Add( currentMetric );
                    }

                    // create values for this metric
                    var metricValue = new MetricValue();
                    metricValue.MetricValueType = MetricValueType.Measure;
                    metricValue.CreatedByPersonAliasId = ImportPersonAliasId;
                    metricValue.CreatedDateTime = ImportDateTime;
                    metricValue.MetricValueDateTime = valueDate;
                    metricValue.MetricId = currentMetric.Id;
                    metricValue.XValue = string.Empty;
                    metricValue.YValue = value.AsDecimalOrNull();
                    metricValue.ForeignKey = string.Format( "Metric Value imported {0}", ImportDateTime );
                    metricValue.Note = metricNote ?? string.Empty;

                    if ( valueDate.HasValue )
                    {
                        var metricPartitionScheduleId = currentMetric.MetricPartitions.FirstOrDefault( p => p.Label == "Service" ).Id;

                        var date = (DateTime)valueDate;
                        var scheduleName = date.DayOfWeek.ToString();

                        if ( date.TimeOfDay.TotalSeconds > 0 )
                        {
                            scheduleName = scheduleName + string.Format( " {0}", date.ToString( "hh:mm" ) ) + string.Format( "{0}", date.ToString( "tt" ).ToLower() );
                        }

                        var metricSchedule = scheduleMetrics.FirstOrDefault( s => s.Name == scheduleName );
                        if ( metricSchedule == null )
                        {
                            metricSchedule = new Schedule();
                            metricSchedule.Name = scheduleName;
                            metricSchedule.iCalendarContent = CreateCalendarContent( date, "WEEKLY", ImportDateTime );
                            metricSchedule.CategoryId = archivedScheduleCategory.Id;
                            metricSchedule.EffectiveStartDate = ImportDateTime;
                            metricSchedule.CreatedByPersonAliasId = ImportPersonAliasId;
                            metricSchedule.CreatedDateTime = ImportDateTime;
                            metricSchedule.ForeignKey = string.Format( "Metric Schedule imported {0}", ImportDateTime );
                            lookupContext.Schedules.Add( metricSchedule );
                            lookupContext.SaveChanges();

                            scheduleMetrics.Add( metricSchedule );
                        }

                        metricValue.MetricValuePartitions.Add( new MetricValuePartition
                        {
                            MetricPartitionId = metricPartitionScheduleId,
                            EntityId = metricSchedule.Id,
                            CreatedDateTime = valueDate,
                            ModifiedDateTime = valueDate,
                            CreatedByPersonAliasId = ImportPersonAliasId,
                            ModifiedByPersonAliasId = ImportPersonAliasId
                        } );
                    }

                    if ( metricCampusId.HasValue && CampusList.Any( c => c.Id == metricCampusId ) )
                    {
                        var metricPartitionCampusId = currentMetric.MetricPartitions.FirstOrDefault( p => p.Label == "Campus" ).Id;
                        metricValue.MetricValuePartitions.Add( new MetricValuePartition { MetricPartitionId = metricPartitionCampusId, EntityId = metricCampusId } );
                    }

                    metricValues.Add( metricValue );

                    completedItems++;
                    if ( completedItems % ( ReportingNumber * 10 ) < 1 )
                    {
                        ReportProgress( 0, string.Format( "{0:N0} metrics imported.", completedItems ) );
                    }
                    else if ( completedItems % ReportingNumber < 1 )
                    {
                        SaveMetrics( metricValues );
                        ReportPartialProgress();

                        metricValues.Clear();
                    }
                }
            }

            // Check to see if any rows didn't get saved to the database
            if ( metricValues.Any() )
            {
                SaveMetrics( metricValues );
            }

            ReportProgress( 0, string.Format( "Finished metrics import: {0:N0} metrics added or updated.", completedItems ) );
            return completedItems;
        }

        /// <summary>
        /// Saves all the metric values.
        /// </summary>
        private void SaveMetrics( List<MetricValue> metricValues )
        {
            var rockContext = new RockContext();
            rockContext.WrapTransaction( () =>
            {
                rockContext.MetricValues.AddRange( metricValues );
                rockContext.SaveChanges( DisableAuditing );
            } );
        }
    }
}