﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Geo.Abstractions.Interfaces;
using Geo.Geometries;

namespace Geo.IO.Wkb
{
    public class WkbReader
    {
        public IOgcGeometry Read(byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException("bytes");

            using (var stream = new MemoryStream(bytes))
                return Read(stream);
        }

        public IOgcGeometry Read(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            using (var reader = new WkbBinaryReader(stream))
            {
                if (!reader.HasData)
                    return null;

                try
                {
                    return ReadGeometry(reader);
                }
                catch (EndOfStreamException)
                {
                    throw new SerializationException("End of stream reached before end of valid WKB geometry.");
                }
            }
        }

        private Coordinate ReadCoordinate(WkbBinaryReader reader, WkbDimensions dimensions)
        {
            var x = reader.ReadDouble();
            var y = reader.ReadDouble();
            var z = dimensions == WkbDimensions.XYZ || dimensions == WkbDimensions.XYZM ? reader.ReadDouble() : Coordinate.NullOrdinate;
            var m = dimensions == WkbDimensions.XYM || dimensions == WkbDimensions.XYZM ? reader.ReadDouble() : Coordinate.NullOrdinate;

            return new Coordinate(y, x, z, m);
        }

        private CoordinateSequence ReadCoordinates(WkbBinaryReader reader, WkbDimensions dimensions)
        {
            var pointCount = (int)reader.ReadUInt32();

            var result = new List<Coordinate>(pointCount);
            for (var i = 0; i < pointCount; i++)
            {
                result.Add(ReadCoordinate(reader, dimensions));
            }

            return new CoordinateSequence(result);
        }

        private IOgcGeometry ReadGeometry(WkbBinaryReader reader)
        {
            var type = reader.ReadUInt32();
            var dimensions = WkbDimensions.XY;
            if (type > 1000)
                dimensions = WkbDimensions.XYZ;
            if (type > 2000)
                dimensions = WkbDimensions.XYM;
            if (type > 3000)
                dimensions = WkbDimensions.XYZM;

            var geometryType = (WkbGeometryType)((int)type % 1000);

            switch (geometryType)
            {
                case WkbGeometryType.Point:
                    return ReadPoint(reader, dimensions);
                case WkbGeometryType.LineString:
                    return ReadLineString(reader, dimensions);
                case WkbGeometryType.Triangle:
                    return ReadTriangle(reader, dimensions);
                case WkbGeometryType.Polygon:
                    return ReadPolygon(reader, dimensions);
                case WkbGeometryType.MultiPoint:
                    return ReadMultiPoint(reader, dimensions);
                case WkbGeometryType.MultiLineString:
                    return ReadMultiLineString(reader, dimensions);
                case WkbGeometryType.MultiPolygon:
                    return ReadMultiPolygon(reader, dimensions);
                case WkbGeometryType.GeometryCollection:
                    return ReadGeometryCollection(reader);
                default:
                    throw new SerializationException("Unknown geometry type.");
            }
        }

        private Point ReadPoint(WkbBinaryReader reader, WkbDimensions dimensions)
        {
            return new Point(ReadCoordinate(reader, dimensions));
        }

        private LineString ReadLineString(WkbBinaryReader reader, WkbDimensions dimensions)
        {
            return new LineString(ReadCoordinates(reader, dimensions));
        }

        private Polygon ReadPolygon(WkbBinaryReader reader, WkbDimensions dimensions)
        {
            var rings = ReadPolygonInner(reader, dimensions);
            if (rings.Count == 0)
                return new Polygon();
            return new Polygon(rings.First(), rings.Skip(1));
        }

        private Polygon ReadTriangle(WkbBinaryReader reader, WkbDimensions dimensions)
        {
            var rings = ReadPolygonInner(reader, dimensions);
            if (rings.Count == 0)
                return new Triangle();
            return new Triangle(rings.First(), rings.Skip(1));
        }

        private List<LinearRing> ReadPolygonInner(WkbBinaryReader reader, WkbDimensions dimensions)
        {
            var result = new List<LinearRing>();
            var ringsCount = (int)reader.ReadUInt32();
            for (var i = 0; i < ringsCount; i++)
                result.Add(new LinearRing(ReadCoordinates(reader, dimensions)));
            return result;
        }

        private MultiPoint ReadMultiPoint(WkbBinaryReader reader, WkbDimensions dimensions)
        {
            var pointsCount = (int)reader.ReadUInt32();
            var points = new List<Point>();
            for (var i = 0; i < pointsCount; i++)
                points.Add(ReadPoint(reader, dimensions));
            return new MultiPoint(points);
        }

        private MultiLineString ReadMultiLineString(WkbBinaryReader reader, WkbDimensions dimensions)
        {
            var pointsCount = (int)reader.ReadUInt32();
            var lineStrings = new List<LineString>();
            for (var i = 0; i < pointsCount; i++)
                lineStrings.Add(ReadLineString(reader, dimensions));
            return new MultiLineString(lineStrings);
        }

        private MultiPolygon ReadMultiPolygon(WkbBinaryReader reader, WkbDimensions dimensions)
        {
            var pointsCount = (int)reader.ReadUInt32();
            var polygons = new List<Polygon>();
            for (var i = 0; i < pointsCount; i++)
                polygons.Add(ReadPolygon(reader, dimensions));
            return new MultiPolygon(polygons);
        }

        private GeometryCollection ReadGeometryCollection(WkbBinaryReader reader)
        {
            var pointsCount = (int)reader.ReadUInt32();
            var geometries = new List<IGeometry>();
            for (var i = 0; i < pointsCount; i++)
                geometries.Add(ReadGeometry(reader));
            return new GeometryCollection(geometries);
        }
    }
}
