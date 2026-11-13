using System.Collections;
using NUnit.Framework;
using QuestBilliards.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace QuestBilliards.Tests.PlayMode
{
    public sealed class MenuBackingTests
    {
        private GameObject head;
        private GameObject eventSystem;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (head != null) Object.Destroy(head);
            if (eventSystem != null) Object.Destroy(eventSystem);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CreateMenuCanvas_WithBacking_UsesCanvasSpaceWithoutPhysicsOrShadows()
        {
            var previousEventSystem = Object.FindAnyObjectByType<EventSystem>();
            head = new GameObject("Head");
            var theme = Resources.Load<UITheme>("UI/UITheme");
            Assert.That(theme, Is.Not.Null);
            Assert.That(theme.menuBackingPrefab, Is.Not.Null);

            var canvas = WorldSpaceUIHelper.CreateMenuCanvas("DifficultySelection", head.transform, theme);
            var currentEventSystem = Object.FindAnyObjectByType<EventSystem>();
            if (previousEventSystem == null && currentEventSystem != null) eventSystem = currentEventSystem.gameObject;

            var renderer = canvas.GetComponentInChildren<MeshRenderer>();
            Assert.That(renderer, Is.Not.Null);
            Assert.That(renderer.transform.parent, Is.EqualTo(canvas.transform));
            Assert.That(renderer.transform.localPosition, Is.EqualTo(new Vector3(0f, 0f, 8f)));
            Assert.That(renderer.transform.localRotation, Is.EqualTo(Quaternion.identity));
            Assert.That(renderer.transform.localScale, Is.EqualTo(Vector3.one / theme.canvasScale));

            var litShader = Shader.Find("Universal Render Pipeline/Lit");
            Assert.That(litShader, Is.Not.Null);
            var renderers = canvas.GetComponentsInChildren<MeshRenderer>(true);
            Assert.That(renderers, Is.Not.Empty);
            foreach (var meshRenderer in renderers)
            {
                Assert.That(meshRenderer.sharedMaterial, Is.Not.Null);
                Assert.That(meshRenderer.shadowCastingMode, Is.EqualTo(ShadowCastingMode.Off));
                Assert.That(meshRenderer.receiveShadows, Is.False);
                foreach (var material in meshRenderer.sharedMaterials)
                {
                    Assert.That(material, Is.Not.Null);
                    Assert.That(material.shader, Is.EqualTo(litShader));
                }
            }

            var meshFilter = renderer.GetComponent<MeshFilter>();
            Assert.That(meshFilter, Is.Not.Null);
            Assert.That(meshFilter.sharedMesh, Is.Not.Null);
            var size = meshFilter.sharedMesh.bounds.size;
            Assert.That(size.x, Is.EqualTo(0.9f).Within(0.01f));
            Assert.That(size.y, Is.EqualTo(0.82f).Within(0.01f));
            Assert.That(canvas.GetComponentsInChildren<Camera>(true), Is.Empty);
            Assert.That(canvas.GetComponentsInChildren<Light>(true), Is.Empty);
            Assert.That(canvas.GetComponentsInChildren<AudioListener>(true), Is.Empty);
            Assert.That(canvas.GetComponentsInChildren<Collider>(true), Is.Empty);
            yield break;
        }
    }
}
