export type PipelineStepResponse = {
  pipelineStepId: string;
  stepName: number;
  status: number;
  attemptCount: number;
  stepData: string | null;
  startedAt: string | null;
  updatedAt: string;
  completedAt: string | null;
  errorMessage: string | null;
};

export const pipelineStepStatusLabels: Record<number, string> = {
  0: "pending",
  1: "running",
  2: "completed",
  3: "failed",
};
