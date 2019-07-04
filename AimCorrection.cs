// ===============================
// AUTHOR   :	Ing. John Dittrich
// CREATE DATE     :    20.05.2019
// NOTE     :  Always include this 
//			   Header when copying 
//						  the file
// ===============================
// Licence: MIT (https://opensource.org/licenses/MIT)

// Permission is hereby granted, free of charge, to any person obtaining a copy of this 
// software and associated documentation files (the "Software"), to deal in the Software 
// without restriction, including without limitation the rights to use, copy, modify, merge, 
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons 
// to whom the Software is furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all copies or 
// substantial portions of the Software.

// Copyright <2019> <Ing. John Dittrich>
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING
// BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ===============================

// How to use: 
// 1) parent an empty (which is the weapon muzzle and should be placed on the position where projectiles
// are spawned) to a weapon.
// 2) parent the weapon to the rotation point (which is the origin)
// 3) drag and drop the empty to the _weaponMuzzle and the rotation point to the _origin
// 4) for testing you can create another empty and drag and drop it to _target field
// 5) press play and move the target

using UnityEngine;

public class AimCorrection : MonoBehaviour
{
	[SerializeField] Transform _origin = null;
	[SerializeField] Transform _target = null;
	[SerializeField] Transform _weaponMuzzle = null;

	[SerializeField] bool _drawDebugInfo = false;
	[SerializeField] bool _aimOnUpdate = true;
	[SerializeField] bool _aimOnLateUpdate = true;

	Quaternion _originOffsetRot;

	void Update()
	{
		if (_aimOnUpdate)
		{
			AimAtTarget();
		}
		if (_drawDebugInfo && _weaponMuzzle)
		{
			Debug.DrawRay(_weaponMuzzle.position, _weaponMuzzle.forward * 200, Color.magenta);
		}
	}

	void LateUpdate()
	{
		if (_aimOnLateUpdate)
		{
			AimAtTarget();
		}
	}

	public void AimAtTarget()
	{
		if (_target)
		{
			Aim(_target.position);
		}
	}

	public void Aim(Vector3 targetPos)
	{
		// origin and weaponMuzzle are needed
		// break here if one is not assigned
		if (!_origin || !_weaponMuzzle)
		{
			this.enabled = false;
			if (!_origin)
			{
				Debug.LogError("origin is not assigned, deactivating AimCorrection");
			}
			else
			{
				Debug.LogError("weaponMuzzle is not assigned, deactivating AimCorrection");
			}
			return;
		}
		// if the target is closer than the weapon, aiming is not possible
		if ((targetPos - _origin.position).sqrMagnitude <=
			(_weaponMuzzle.position - _origin.position).sqrMagnitude)
		{
			// aiming not possible
			return;
		}
		// calculate the offset rotation (so we can use Quaternion.LookRotation later)
		_originOffsetRot = Quaternion.Inverse(_weaponMuzzle.rotation) * _origin.rotation;

		// get the local vector of _origin-_weaponMuzzle and set its z value to zero
		// we need this position for the following calculations
		Vector3 muzzleLocalOrigin = _weaponMuzzle.InverseTransformDirection(
			_origin.position - _weaponMuzzle.position);
		muzzleLocalOrigin.z = 0;

		// the aligned weapon muzzle position is on the same local z position as the origin
		Vector3 weaponMuzzleAlignedPos = _origin.position - muzzleLocalOrigin;

		Vector3 resultVector = Vector3.zero;


		// horizontal
		{
			// get the XZ vectors here for the horizontal alginment
			Vector3 weaponMuzzle_xz = weaponMuzzleAlignedPos,
				transform_xz = _origin.position,
				targetPos_xz = targetPos,
				muzzleLocalOrigin_xz = muzzleLocalOrigin;
			weaponMuzzle_xz.y = transform_xz.y = targetPos_xz.y = muzzleLocalOrigin_xz.y = 0;

			// c-side of the triangle / hypotenuse
			Vector3 c_Vec = targetPos_xz - transform_xz;

			// calculate alpha with the sine rule
			float a, c, alpha;
			a = (muzzleLocalOrigin_xz).magnitude;
			c = c_Vec.magnitude;
			alpha = Mathf.Asin(a / c);

			// invert alpha when local x is positive
			if (muzzleLocalOrigin.x > 0)
			{
				alpha *= -1;
			}

			// generate the quaternion and apply the rotation to the hypotenuse, 
			// so we get the wanted point C
			Quaternion c_rot = Quaternion.Euler(0, Mathf.Rad2Deg * -alpha, 0);
			c_Vec = c_rot * c_Vec;
			resultVector = c_Vec;

			// debug
			if (_drawDebugInfo)
			{
				DrawAxisCross(_origin.position + c_Vec, 2, Color.green);
			}
		}


		// vertical
		{
			// get the horizontal originPosition
			Vector3 origin_xz = _origin.position;
			origin_xz.y = 0;

			// get the current horizontal aim target and add the y value of the real target
			// so we can form a triangle again, but in vertical direction
			Vector3 horizontalAimTarget = origin_xz + resultVector;
			Vector3 offsetAimTarget = horizontalAimTarget + new Vector3(0, targetPos.y, 0);

			// calculate the rotation axis of alpha
			Vector3 AB = horizontalAimTarget - origin_xz;
			Vector3 perAB = new Vector3(AB.z, 0, -AB.x); // perpendicular AB

			// c-side of the triangle / hypotenuse
			Vector3 c_Vec = offsetAimTarget - _origin.position;

			// calculate alpha with the sine rule
			float a, c, alpha;
			a = -muzzleLocalOrigin.y;
			c = c_Vec.magnitude;
			alpha = Mathf.Asin(a / c);

			// generate the quaternion and apply the rotation to the hypotenuse, 
			// so we get the wanted point C
			Quaternion c_rot = Quaternion.AngleAxis(Mathf.Rad2Deg * alpha, perAB);
			c_Vec = c_rot * c_Vec;
			resultVector = c_Vec;

			// debug
			if (_drawDebugInfo)
			{
				DrawAxisCross(_origin.position + resultVector, 2, Color.cyan);
			}
		}


		// Look at the result direction (resultVector) and apply the offset
		Quaternion resultRotation = Quaternion.LookRotation(resultVector, Vector3.up);
		_origin.rotation = resultRotation * _originOffsetRot;
	}

	void DrawAxisCross(Vector3 point, float size, Color color)
	{
		Debug.DrawLine(point + Vector3.up * size, point - Vector3.up * size, color);
		Debug.DrawLine(point + Vector3.right * size, point - Vector3.right * size, color);
		Debug.DrawLine(point + Vector3.forward * size, point - Vector3.forward * size, color);
	}
}
