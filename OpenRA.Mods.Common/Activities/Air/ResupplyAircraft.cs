#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class ResupplyAircraft : Activity
	{
		readonly Aircraft aircraft;
		Activity inner;

		public ResupplyAircraft(Actor self)
		{
			aircraft = self.Trait<Aircraft>();
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled)
				return NextActivity;

			if (inner == null)
			{
				var host = aircraft.GetActorBelow();

				if (host == null)
					return NextActivity;

				if (aircraft.IsPlane)
				{
					inner = ActivityUtils.SequenceActivities(
						aircraft.GetResupplyActivities(host)
						.Append(new AllowYieldingReservation(self))
						.Append(new WaitFor(() => NextInQueue != null || aircraft.ReservedActor == null))
						.ToArray());
				}
				else
				{
					// Helicopters should take off from their helipad immediately after resupplying.
					// HACK: Append NextInQueue to TakeOff to avoid moving to the Rallypoint (if NextInQueue is non-null).
					inner = ActivityUtils.SequenceActivities(
						aircraft.GetResupplyActivities(host)
						.Append(new AllowYieldingReservation(self))
						.Append(new TakeOff(self)).Append(NextInQueue).ToArray());
				}
			}
			else
				inner = ActivityUtils.RunActivity(self, inner);

			// The inner == NextInQueue check is needed here because of the TakeOff issue mentioned in the comment above.
			return inner == null || inner == NextInQueue ? NextActivity : this;
		}

		public override bool Cancel(Actor self)
		{
			if (!IsCanceled && inner != null && !inner.Cancel(self))
				return false;

			return base.Cancel(self);
		}
	}
}
