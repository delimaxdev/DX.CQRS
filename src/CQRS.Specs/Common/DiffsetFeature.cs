using DX.Cqrs.Commons;
using DX.Testing;
using FluentAssertions;
using System;
using System.Collections.Generic;
using Xbehave;

namespace Common {
    public class DiffsetFeature : Feature {
        [Scenario]
        internal void Create(
            Diffset<string, string> diffset,
            Dictionary<string, string> original,
            Dictionary<string, string> modified,
            List<(char Type, string Key, string OldValue, string NewValue)> expected,
            List<(char Type, string Key, string OldValue, string NewValue)> actual
        ) {
            GIVEN["two dictionaries of items"] = () => {
                string unchangedValue;
                string changedOldValue;
                string changedNewValue;
                string removedValue;
                string addedValue;

                original = new Dictionary<string, string> {
                    ["UNCHANGED"] = unchangedValue = "Contained in both",
                    ["CHANGED"] = changedOldValue = "Old value",
                    ["REMOVED"] = removedValue = "Not contained in modified"
                };

                modified = new Dictionary<string, string> {
                    ["UNCHANGED"] = unchangedValue,
                    ["CHANGED"] = changedNewValue = "New value",
                    ["ADDED"] = addedValue = "Only contained in modified"
                };

                expected = new List<(char, string, string, string)> {
                    ('U', "UNCHANGED", unchangedValue, unchangedValue),
                    ('C', "CHANGED", changedOldValue, changedNewValue),
                    ('R', "REMOVED", removedValue, null),
                    ('A', "ADDED", null, addedValue)
                };
            };

            WHEN["creating the diffset"] = () => diffset = Diffset.Create(original, modified);

            AND["applying it"] = () => {
                actual = new List<(char, string, string, string)>();
                diffset.Apply(c => {
                    c.Added((key, value) => actual.Add(('A', key, null, value)));
                    c.Removed((key, value) => actual.Add(('R', key, value, null)));
                    c.Unchanged((key, value) => actual.Add(('U', key, value, value)));
                    c.Changed((key, oldValue, newValue) => actual.Add(('C', key, oldValue, newValue)));
                });
            };

            THEN["it applies the correct differences"] = () =>
                actual.Should().BeEquivalentTo(expected, o => o.WithoutStrictOrdering());
        }

        private class KeyEqualityComparer : IEqualityComparer<Key> {
            public bool Equals(Key x, Key y)
                => x?.Value == y?.Value;

            public int GetHashCode(Key obj)
                => HashCode.Combine(obj?.Value);
        }

        private class Key {
            public string Value { get; }

            public Key(string value)
                => Value = value;
        }
    }
}