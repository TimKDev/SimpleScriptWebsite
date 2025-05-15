#include <stdio.h>
#include <string.h>
#include <stdlib.h>
#include <ctype.h>

typedef struct Node
{
    void* data;
    struct Node* next;
} Node;

Node* head = NULL;

void add_to_list(void* data)
{
    Node* new_node = (Node*)malloc(sizeof(Node));
    new_node->data = data;
    new_node->next = head;
    head = new_node;
}

void free_list()
{
    Node* current = head;
    while (current != NULL)
    {
        Node* next_node = current->next;
        free(current->data);
        free(current);
        current = next_node;
    }
    head = NULL;
}

int ToNumber(const char* str)
{
    if (str == NULL)
    {
        return 0;
    }

    // Skip leading whitespace
    while (isspace(*str))
    {
        str++;
    }

    // Check if the string is empty after removing whitespace
    if (*str == '\0')
    {
        return 0;
    }

    // Convert string to integer using strtol
    char* endptr;
    int result = (int)strtol(str, &endptr, 10);

    // Check for conversion errors
    if (*endptr != '\0')
    {
        printf("Error: Invalid characters in input string\n");
        return 0;
    }

    return result;
}
