﻿namespace Signify.FOBT.Svc.System.Tests.Core.Exceptions;

public class NotPerformedNotFoundException(string message) : Exception(message);