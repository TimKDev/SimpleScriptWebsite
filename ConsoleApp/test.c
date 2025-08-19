

#include "CCode/compiler-helper.h"
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
char *addStrings(char string_1[], char string_2[]) {
  char *temp_1 =
      (char *)malloc((strlen(string_1) + strlen(string_2) + 1) * sizeof(char));
  add_to_list(temp_1);
  strcpy(temp_1, string_1);

  strcat(temp_1, string_2);

  char *result = temp_1;
  return result;
}
int main() {
  printf("%s\n", addStrings("Hallo ", "String Addition"));
  free_list();
  return 0;
}
