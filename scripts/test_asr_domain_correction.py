#!/usr/bin/env python3
import unittest

from asr_domain_correction import correct_text


class AsrDomainCorrectionTests(unittest.TestCase):
    def test_corrects_known_upper_code_candidate(self) -> None:
        dictionary = {
            "animal_tags": ["A-17"],
            "proper_nouns": [],
            "terms": [],
        }

        corrected, corrections = correct_text("Посмотри родителей УАЗ-17.", dictionary)

        self.assertEqual(corrected, "Посмотри родителей A-17.")
        self.assertEqual(corrections[0]["stage"], "constrained_llm_candidate")

    def test_does_not_fuzzy_correct_short_numeric_tags(self) -> None:
        dictionary = {
            "animal_tags": ["77"],
            "proper_nouns": [],
            "terms": [],
        }

        corrected, corrections = correct_text("Родители у БК-707 есть?", dictionary)

        self.assertEqual(corrected, "Родители у БК - 707 есть?")
        self.assertEqual(corrections, [])

    def test_compacts_spaced_digits_only_when_exact_dictionary_match_exists(self) -> None:
        dictionary = {
            "animal_tags": ["1432"],
            "proper_nouns": [],
            "terms": [],
        }

        corrected, corrections = correct_text("найди бирку 14 32", dictionary)

        self.assertEqual(corrected, "найди бирку 1432")
        self.assertEqual(corrections[0]["type"], "spaced_digits")


if __name__ == "__main__":
    unittest.main()
