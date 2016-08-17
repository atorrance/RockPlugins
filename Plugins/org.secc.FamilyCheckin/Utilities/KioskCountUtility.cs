﻿using Rock;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rock.CheckIn;
using Rock.Web.Cache;
using Rock.Data;
using Rock.Model;

namespace org.secc.FamilyCheckin.Utilities
{
    public class KioskCountUtility
    {
        public List<GroupType> GroupTypes { get; private set; }
        public List<int> VolunteerGroupIds { get; private set; }
        public List<int> ChildGroupIds { get; private set; }
        public List<GroupLocationSchedule> GroupLocationSchedules { get; private set; }

        public KioskCountUtility( List<int> ConfiguredGroupTypes, Guid VolunteerAttributeGuid )
        {
            _KioskCountUtility( ConfiguredGroupTypes, VolunteerAttributeGuid, null );
        }

        public KioskCountUtility( List<int> ConfiguredGroupTypes, Guid VolunteerAttributeGuid, Guid DeactivatedDefinedTypeGuid )
        {
            _KioskCountUtility( ConfiguredGroupTypes, VolunteerAttributeGuid, DeactivatedDefinedTypeGuid );
        }

        private void _KioskCountUtility( List<int> ConfiguredGroupTypes, Guid VolunteerAttributeGuid, Guid? DeactivatedDefinedTypeGuid )
        {
            RockContext rockContext = new RockContext();

            IQueryable<GroupType> groupTypeService = new GroupTypeService( rockContext ).Queryable( "Groups,Groups.GroupLocations,Groups.GroupLocations.Schedules" );

            GroupTypes = groupTypeService
                .Where( gt => ConfiguredGroupTypes.Contains( gt.Id ) ).ToList();

            foreach ( var gt in GroupTypes )
            {
                foreach ( var g in gt.Groups )
                {
                    g.LoadAttributes();
                }
            }

            //if ( GroupTypes.Any() )
            //{
            //    var allGroupTypes = groupTypeService.Where( gt => gt.ChildGroupTypes.Contains( GroupTypes.FirstOrDefault() ) )
            //        .Select( gt => gt.ChildGroupTypes.Where( cgt => !ConfiguredGroupTypes.Contains( cgt.Id ) ) ).ToList();

            //    foreach ( var gt in allGroupTypes )
            //    {
            //        foreach ( var g in gt.Groups )
            //        {
            //            g.LoadAttributes();
            //        }
            //    }
            //}



            var volAttributeKey = AttributeCache.Read( VolunteerAttributeGuid ).Key;

            VolunteerGroupIds = GroupTypes
                .SelectMany( gt => gt.Groups )
                .Where( g => g.GetAttributeValue( volAttributeKey ).AsBoolean() )
                .Select( g => g.Id ).ToList();

            ChildGroupIds = GroupTypes
                .SelectMany( gt => gt.Groups )
                .Where( g => !g.GetAttributeValue( volAttributeKey ).AsBoolean() )
                .Select( g => g.Id ).ToList();

            var groupLocations = GroupTypes
                .SelectMany( gt => gt.Groups )
                .SelectMany( g => g.GroupLocations );

            GroupLocationSchedules = new List<GroupLocationSchedule>();

            foreach ( var gl in groupLocations )
            {
                foreach ( var s in gl.Schedules )
                {
                    GroupLocationSchedules.Add( new GroupLocationSchedule( gl, s, true ) );
                }
            }

            if ( DeactivatedDefinedTypeGuid != null )
            {
                GroupLocationService groupLocationService = new GroupLocationService( rockContext );
                ScheduleService scheduleService = new ScheduleService( rockContext );

                var dtDeactivated = DefinedTypeCache.Read( DeactivatedDefinedTypeGuid ?? new Guid() );
                var dvDeactivated = dtDeactivated.DefinedValues;
                GroupLocationSchedules.AddRange( dvDeactivated.Select( dv => dv.Value.Split( '|' ) )
                    .Select( s => new GroupLocationSchedule(
                        groupLocationService.Get( s[0].AsInteger() ),
                        scheduleService.Get( s[1].AsInteger() ),
                        false )
                    ).ToList()
                );
            }
        }

        public LocationScheduleCount GetLocationScheduleCount( int LocationId, int ScheduleId )
        {
            var locationScheduleCount = new LocationScheduleCount();
            var kgas = KioskLocationAttendance.Read( LocationId ).Groups.Where( g => g.Schedules.Where( s => s.ScheduleId == ScheduleId ).Any() ).ToList();

            locationScheduleCount.ChildCount = kgas.Where( kga => ChildGroupIds.Contains( kga.GroupId ) )
                .SelectMany( kga => kga.Schedules.Where( s => s.ScheduleId == ScheduleId ) )
                .Select( kgs => kgs.CurrentCount ).Sum();

            locationScheduleCount.VolunteerCount = kgas.Where( kga => VolunteerGroupIds.Contains( kga.GroupId ) )
                .SelectMany( kga => kga.Schedules.Where( s => s.ScheduleId == ScheduleId ) )
                .Select( kgs => kgs.CurrentCount ).Sum();

            locationScheduleCount.ReservedCount = 0;

            locationScheduleCount.TotalCount = kgas
                .SelectMany( kga => kga.Schedules.Where( s => s.ScheduleId == ScheduleId ) )
                .Select( kgs => kgs.CurrentCount ).Sum();

            return locationScheduleCount;
        }

        public class GroupLocationSchedule
        {
            public GroupLocation GroupLocation { get; private set; }
            public Schedule Schedule { get; private set; }
            public bool Active { get; private set; }

            internal GroupLocationSchedule( GroupLocation GroupLocation, Schedule Schedule, bool Active )
            {
                this.GroupLocation = GroupLocation;
                this.Schedule = Schedule;
                this.Active = Active;
            }
        }

        public class LocationScheduleCount
        {
            public int ChildCount { get; internal set; }
            public int VolunteerCount { get; internal set; }
            public int ReservedCount { get; internal set; }
            public int TotalCount { get; internal set; }

        }
    }
}