import argparse

parser = argparse.ArgumentParser(description="Confocal script")
parser.add_argument("--light-filter", type=str, help="enum [Reflection|Fluorescence]")
parser.add_argument("--laser-color", type=float, help="light frequency in nanometers")
parser.add_argument("--intensity", type=int, help="light intensity in percent")
parser.add_argument("--dwell", type=float, help="duration in nanoseconds")
parser.add_argument("--field-of-view-height", type=float, help="field of view height in nanometers")
parser.add_argument("--field-of-view-width", type=float, help="field of view width in nanometers")
parser.add_argument("--pixel-size", type=float, help="pixel size in nanometers")
parser.add_argument("--gain", type=float, help="gain in decibels")
parser.add_argument("--adc", type=float, help="adc in volts")
parser.add_argument("--image-path", type=str, help="output image full path")

args = parser.parse_args()
