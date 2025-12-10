using System.Reactive;
using System.Reactive.Subjects;
using Tarmi.Models;
using DynamicData;
using Microsoft.Extensions.Logging;

namespace Tarmi.Projects.Implementation;
public class FiducialsManager
{
    private readonly Project _project;
    private readonly ProjectManager _projectManager;
    private readonly ILogger _logger;

    private readonly Subject<Unit> _fiducialsUpdateSubject = new();

    private CorrelationInfo? _reference;
    private CorrelationInfo? _addingTo;

    public IObservable<Unit> FiducialsUpdate => _fiducialsUpdateSubject;

    public bool HasReferenceImage => _reference != null;

    public FiducialsManager(ProjectManager projectManager, Project project, ILogger logger)
    {
        _projectManager = projectManager;
        _project = project;
        _logger = logger;
    }

    private void SaveProjectAndUpdate()
    {
        _projectManager.SaveProject(_project);
        _logger.Swallow(() => _fiducialsUpdateSubject.OnNext(Unit.Default));
    }

    public void AddFiducial(RegionOfInterest regionOfInterest, LengthPoint position)
    {
        if (_reference is null)
        {
            _logger.LogError("Attempt to add fiducial without reference correlation info.");
            return;
        }
        // TODO: Add suitable name
        var reference = new FiducialDescriptor()
        {
            Name = $"Fiducial {regionOfInterest.Fiducials.Count + 1}"
        };
        regionOfInterest.Fiducials.Add(reference);
        var fiducial = FiducialPoint.FromPoint(position, reference.Id);
        if (_reference.FiducialsGroupId.IsEmpty())
        {
            _reference.FiducialPoints.Add(fiducial);
        }
        else
        {
            CommitAction(regionOfInterest, correlationInfo =>
            {
                if (correlationInfo.FiducialsGroupId == _reference.FiducialsGroupId)
                {
                    var fiducial = FiducialPoint.FromPoint(position, reference.Id);
                    correlationInfo.FiducialPoints.Add(fiducial);
                }
            });
        }

        if (_addingTo is not null)
        {
            if (_addingTo.FiducialsGroupId.IsEmpty())
            {
                _addingTo.FiducialPoints?.Add(fiducial);
            }
            else if (_reference.FiducialsGroupId != _addingTo.FiducialsGroupId)
            {
                CommitAction(regionOfInterest, correlationInfo =>
                {
                    if (correlationInfo.FiducialsGroupId == _addingTo.FiducialsGroupId)
                    {
                        var fiducial = FiducialPoint.FromPoint(position, reference.Id);
                        correlationInfo.FiducialPoints.Add(fiducial);
                    }
                });
            }
        }
        SaveProjectAndUpdate();
    }

    public void RemoveFiducial(RegionOfInterest regionOfInterest, Guid fiducialId)
    {
        _ = regionOfInterest.Fiducials.RemoveAll(fiducial => fiducial.Id == fiducialId);
        CommitAction(regionOfInterest, correlationInfo => correlationInfo.FiducialPoints.RemoveAll(fiducial => fiducial.Id == fiducialId));
        SaveProjectAndUpdate();
    }

    public void RenameFiducial(RegionOfInterest regionOfInterest, Guid fiducialId, string newName)
    {
        var fiducial = regionOfInterest.Fiducials.Find(regionOfInterest => regionOfInterest.Id == fiducialId);
        if (fiducial is null)
        {
            _logger.LogError("Fiducial with {ID} not found.", fiducialId);
            return;
        }
        fiducial.Name = newName;
        SaveProjectAndUpdate();
    }

    public void UpdateFiducial(RegionOfInterest regionOfInterest, CorrelationInfo correlationInfo, FiducialPoint updatedFiducial)
    {
        if (correlationInfo.IsReferenceImage)
        {
            var index = correlationInfo.FiducialPoints.FindIndex(fiducial => fiducial.Id == updatedFiducial.Id);
            var original = correlationInfo.FiducialPoints[index];
            var xOffset = updatedFiducial.Position.X - original.Position.X;
            var yOffset = updatedFiducial.Position.Y - original.Position.Y;
            CommitAction(regionOfInterest, ci =>
            {
                var index = ci.FiducialPoints.FindIndex(fiducial => fiducial.Id == updatedFiducial.Id);
                if (index == -1)
                {
                    return;
                }
                var fiducial = ci.FiducialPoints[index];
                ci.FiducialPoints[index] = fiducial with
                {
                    Position = new LengthPoint()
                    {
                        X = fiducial.Position.X + xOffset,
                        Y = fiducial.Position.Y + yOffset
                    },
                };
            });
        }
        else if (correlationInfo.FiducialsGroupId.IsEmpty())
        {
            var index = correlationInfo.FiducialPoints.FindIndex(fiducial => fiducial.Id == updatedFiducial.Id);
            correlationInfo.FiducialPoints[index] = updatedFiducial;
        }
        else
        {
            CommitAction(regionOfInterest, ci =>
            {
                if (ci.FiducialsGroupId == correlationInfo.FiducialsGroupId)
                {
                    var index = ci.FiducialPoints.FindIndex(fiducial => fiducial.Id == updatedFiducial.Id);
                    ci.FiducialPoints[index] = updatedFiducial;
                }
            });
        }
        SaveProjectAndUpdate();
    }

    public void Bind(CorrelationInfo parentInfo, CorrelationInfo childInfo)
    {
        if (childInfo.FiducialsGroupId.IsNotEmpty())
        {
            // Unbind instead?
            throw new InvalidOperationException("Cannot bind an already bound correlation info.");
        }
        // Create new group if it does not already exist.
        if (parentInfo.FiducialsGroupId.IsEmpty())
        {
            parentInfo.FiducialsGroupId = Guid.NewGuid();
        }
        childInfo.FiducialsGroupId = parentInfo.FiducialsGroupId;
        EnsureFiducialsConsistency(childInfo, parentInfo);
        SaveProjectAndUpdate();
    }

    public void Unbind(RegionOfInterest regionOfInterest, CorrelationInfo correlationInfo)
    {
        var id = correlationInfo.FiducialsGroupId;
        correlationInfo.FiducialsGroupId = Guid.Empty;
        List<CorrelationInfo> group = [];
        CommitAction(regionOfInterest, ci =>
        {
            if (ci.FiducialsGroupId == id)
            {
                group.Add(ci);
            }
        });
        // Delete group id single remaining member.
        if (group.Count == 1)
        {
            group[0].FiducialsGroupId = Guid.Empty;
        }
        SaveProjectAndUpdate();
    }


    public void PrepareFiducials(RegionOfInterest regionOfInterest, CorrelationInfo correlationInfo)
    {
        if (_reference is null)
        {
            return;
        }
        if (correlationInfo.FiducialsGroupId.IsEmpty())
        {
            EnsureFiducialsConsistency(correlationInfo, _reference);
        }
        else
        {
            CommitAction(regionOfInterest, ci =>
            {
                if (ci.FiducialsGroupId == correlationInfo.FiducialsGroupId)
                {
                    EnsureFiducialsConsistency(ci, _reference);
                }
            });
        }
        SaveProjectAndUpdate();
    }

    private static void EnsureFiducialsConsistency(CorrelationInfo correlationInfo, CorrelationInfo reference)
    {
        var toRemove = correlationInfo.FiducialPoints
            .ExceptBy(reference.FiducialPoints.Select(point => point.Id), point => point.Id);
        var toAdd = reference.FiducialPoints
            .ExceptBy(correlationInfo.FiducialPoints.Select(point => point.Id), point => point.Id);
        
        correlationInfo.FiducialPoints.RemoveMany(toRemove);
        correlationInfo.FiducialPoints.AddRange(toAdd);
    }

    private void CommitAction(RegionOfInterest regionOfInterest, Action<CorrelationInfo> action)
    {
        var correlationInfos = regionOfInterest.Images
            .SelectMany(layer => layer.Images.Select(image => image.CorrelationInfo))
            .Concat(regionOfInterest.TileSets.Select(tileset => tileset.CorrelationInfo))
            .Concat(regionOfInterest.TileSets3D.Select(tileset3D => tileset3D.CorrelationInfo))
            .Concat(regionOfInterest.ZStacks.Select(zStack => zStack.CorrelationInfo));
        
        foreach (var correlationInfo in correlationInfos)
        {
            _logger.LogInformation("Correlation info: {GroupID}, {Points}", correlationInfo.FiducialsGroupId, correlationInfo.FiducialPoints);
            action.Invoke(correlationInfo);
        }
    }

    public void UpdateActiveRoi(RegionOfInterest activeRoi)
    {
        _reference = null;
        CommitAction(activeRoi, correlationInfo =>
        {
            if (correlationInfo is { IsReferenceImage: true })
            {
                _reference = correlationInfo;
            }
        });
    }

    public void SetReference(RegionOfInterest regionOfInterest, CorrelationInfo? correlationInfo)
    {
        regionOfInterest.Fiducials?.Clear();
        CommitAction(regionOfInterest, correlationInfo => correlationInfo.FiducialPoints?.Clear());
        _reference?.IsReferenceImage = false;
        
        _reference = correlationInfo;
        _reference?.IsReferenceImage = true;
        _reference?.FiducialPoints?.Clear();

        SaveProjectAndUpdate();
    }

    internal void SetAddingCorrelationInfo(CorrelationInfo? correlationInfo)
    {
        if (correlationInfo is null || _reference != correlationInfo)
        {
            _addingTo = correlationInfo;
        }
    }
}
