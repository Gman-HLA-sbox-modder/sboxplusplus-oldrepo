﻿using Sandbox;

[Library( "weapon_npc", Title = "NPC Test Pistol", Spawnable = false )]
partial class NPCPistol : Weapon
{
	public override string ViewModelPath => "weapons/rust_pistol/v_rust_pistol.vmdl";

	public override float PrimaryRate => 15.0f;
	public override float SecondaryRate => 1.0f;

	public TimeSince TimeSinceDischarge { get; set; }

	public override void Spawn()
	{
		base.Spawn();

		SetModel( "weapons/rust_pistol/rust_pistol.vmdl" );
	}

	public override bool CanPrimaryAttack()
	{
		return base.CanPrimaryAttack() && Input.Pressed( InputButton.Attack1 );
	}

	public override void AttackPrimary()
	{
		TimeSincePrimaryAttack = 0;
		TimeSinceSecondaryAttack = 0;

		//
		// Tell the clients to play the shoot effects
		//
		ShootEffects();
		//PlaySound( "rust_pistol.shoot" );


		// spawn npc
		if (!Host.IsServer) return;

		var startPos = Owner.EyePos;
		var dir = Owner.EyeRot.Forward;
		var tr = Trace.Ray(startPos, startPos + dir * 5000)
					.Ignore(Owner)
					.Run();

		var npc = new NpcTest
		{
			Position = tr.EndPos,
			//Position = Owner.Position,

			Rotation = Rotation.LookAt(Owner.EyeRot.Backward.WithZ(0))

		};

		npc.Tags.Add("selectable");
		npc.Tags.Add("npc");

	}

	public override void AttackSecondary()
	{
		if (!Host.IsServer) return;

		var HasLos = true;
		var SpawnPos = Position;
		var Tries = 0;

		while (HasLos && Tries <= 50)
		{
			Tries += 1;
			var t = NavMesh.GetPointWithinRadius(Position, 1000, 4000);
			if (t.HasValue)
			{
				SpawnPos = t.Value;
				var AddHeight = new Vector3(0, 0, 70);

				var PlayerPos = Owner.EyePos;
				var tr = Trace.Ray(SpawnPos + AddHeight, PlayerPos)
							.UseHitboxes()
							.Run();

				if (Vector3.DistanceBetween(tr.EndPos, PlayerPos) > 100)
				{
					HasLos = false;
					//Log.Info("no los");
					//var npc1 = new NpcTest{Position = tr.EndPos,};
				}
				else
				{
					Log.Warning("Can't Find Valid Zombie Spawn");
				}


			}
		}
	}

	private void Discharge()
	{
		if ( TimeSinceDischarge < 0.5f )
			return;

		TimeSinceDischarge = 0;

		var muzzle = GetAttachment( "muzzle" ) ?? default;
		var pos = muzzle.Position;
		var rot = muzzle.Rotation;

		ShootEffects();
		PlaySound( "rust_pistol.shoot" );
		ShootBullet( pos, rot.Forward, 0.05f, 1.5f, 9.0f, 3.0f );

		ApplyAbsoluteImpulse( rot.Backward * 200.0f );
	}

	protected override void OnPhysicsCollision( CollisionEventData eventData )
	{
		if ( eventData.Speed > 500.0f )
		{
			Discharge();
		}
	}

}
