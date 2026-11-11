using UnityEngine;

namespace EchoesOfTheFog.Combat
{
    public sealed class RifleView : MonoBehaviour
    {
        [SerializeField] private Transform muzzle;
        [SerializeField] private Transform leftHandGrip;
        public Transform Muzzle => muzzle;
        public Transform LeftHandGrip => leftHandGrip;
    }
}
