import { QuestionType } from '../enums';

export interface Question {
  key: string;
  label: string;
  kind: QuestionType;
  helpText: string | null;
  min: number | null;
  max: number | null;
  options: string[];
}
