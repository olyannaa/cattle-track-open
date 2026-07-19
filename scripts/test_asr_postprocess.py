#!/usr/bin/env python3
import unittest

from asr_postprocess import normalize_asr_text


class AsrPostprocessTests(unittest.TestCase):
    def test_normalize_number_words(self) -> None:
        self.assertEqual(normalize_asr_text("родители у быка семьдесят семь есть"), "родители у быка 77 есть")
        self.assertEqual(normalize_asr_text("открой карточку пятьсот двадцать три"), "открой карточку 523")
        self.assertEqual(normalize_asr_text("найди тысяча четыреста тридцать два"), "найди 1432")
        self.assertEqual(normalize_asr_text("переведи девять тысяч девятьсот девяносто девять"), "переведи 9999")

    def test_normalize_latin_cyrillic_codes(self) -> None:
        self.assertEqual(normalize_asr_text("Карточку по номеру А17 открой."), "Карточку по номеру A-17 открой.")
        self.assertEqual(normalize_asr_text("эмбрион Е99 для А 17 сегодня"), "эмбрион E-99 для A-17 сегодня")
        self.assertEqual(normalize_asr_text("внеси эмбрион E 55 для 981"), "внеси эмбрион E-55 для 981")

    def test_keeps_regular_russian_words(self) -> None:
        self.assertEqual(normalize_asr_text("покажи коров на проверку стельности"), "покажи коров на проверку стельности")


if __name__ == "__main__":
    unittest.main()
